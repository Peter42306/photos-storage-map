using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Infrastructure.Data;
using System.Security.Claims;
using PhotosStorageMap.Domain.Entities;
using System.Net.WebSockets;
using Amazon.Runtime.CredentialManagement;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Domain.Enums;
using PhotosStorageMap.Application.Common;

namespace PhotosStorageMap.Api.Controllers
{
    [Route("api/share-links")]
    [ApiController]
    [Authorize]
    public class ShareLinksController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileStorage _storage;
        private readonly IConfiguration _configuration;
        private readonly IArchiveCollectionService _archiveCollectionService;
        private readonly IZipJobStore _zipJobStore;
        private readonly ILogger<ShareLinksController> _logger;
        
        public ShareLinksController(
            ApplicationDbContext db,
            IFileStorage storage,
            IConfiguration configuration,
            IArchiveCollectionService archiveCollectionService,
            IZipJobStore zipJobStore,
            ILogger<ShareLinksController> logger)
        {
            _db = db;
            _storage = storage;
            _configuration = configuration;
            _archiveCollectionService = archiveCollectionService;
            _zipJobStore = zipJobStore;
            _logger = logger;
        }


        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(
            [FromBody] CreateOrUpdateShareLinkRequest request,
            CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();            

            var collection = await _db.UploadCollections
                .FirstOrDefaultAsync(c => 
                    c.Id == request.CollectionId && 
                    c.OwnerUserId == userId &&
                    !c.IsDeleted,
                    ct);

            if (collection is null) return NotFound("Collection not found.");

            var shareLink = await _db.ShareLinks.SingleOrDefaultAsync(s => s.UploadCollectionId == collection.Id, ct);

            var token = Guid.NewGuid().ToString("N");

            if (shareLink is null)
            {
                shareLink = new ShareLink
                {
                    UploadCollectionId = collection.Id,
                    Token = token,
                    CreatedAtUtc = DateTime.UtcNow,
                    IsRevoked = false,
                    AllowSlideshowOriginals = request.AllowSlideshowOriginals,
                    AllowDownloadResizedZip = request.AllowDownloadResizedZip,
                    AllowDownloadOriginalFromCard = request.AllowDownloadOriginalFromCard
                };

                _db.ShareLinks.Add(shareLink);
            }
            else
            {
                shareLink.Token = token;
                shareLink.CreatedAtUtc = DateTime.UtcNow;
                shareLink.IsRevoked = false;
                shareLink.AllowSlideshowOriginals = request.AllowSlideshowOriginals;
                shareLink.AllowDownloadResizedZip = request.AllowDownloadResizedZip;
                shareLink.AllowDownloadOriginalFromCard = request.AllowDownloadOriginalFromCard;
            }           

            await _db.SaveChangesAsync(ct);            

            var url = BuildShareLink(shareLink.Token);
            
            return Ok(new ShareLinkResponse(
                shareLink.Id,
                shareLink.Token,
                url,
                shareLink.CreatedAtUtc,
                shareLink.IsRevoked,
                shareLink.AllowSlideshowOriginals,
                shareLink.AllowDownloadResizedZip,
                shareLink.AllowDownloadOriginalFromCard
            ));
        }


        [HttpGet("collection/{collectionId:guid}")]
        public async Task<IActionResult> GetByCollectionId(
            Guid collectionId,
            CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var shareLink = await _db.ShareLinks                
                .Include(s => s.UploadCollection)
                .SingleOrDefaultAsync(s => 
                    s.UploadCollectionId == collectionId &&
                    s.UploadCollection.OwnerUserId == userId &&
                    !s.UploadCollection.IsDeleted,
                    ct);

            if (shareLink == null) 
            {
                return Ok(null);
            }

            var url = BuildShareLink(shareLink.Token);

            return Ok(new ShareLinkResponse(
                shareLink.Id,
                shareLink.Token,
                url,
                shareLink.CreatedAtUtc,
                shareLink.IsRevoked,
                shareLink.AllowSlideshowOriginals,
                shareLink.AllowDownloadResizedZip,
                shareLink.AllowDownloadOriginalFromCard
            ));
        }


        [HttpPost("{id:guid}/revoke")]
        public async Task<IActionResult> Revoke(
            Guid id,
            CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var shareLink = await _db.ShareLinks
                .Include(s => s.UploadCollection)
                .SingleOrDefaultAsync(s =>
                    s.Id == id &&
                    s.UploadCollection.OwnerUserId == userId &&
                    !s.UploadCollection.IsDeleted,
                    ct);

            if (shareLink == null) return NotFound();

            shareLink.IsRevoked = true;

            await _db.SaveChangesAsync(ct);

            return NoContent();
        }


        [AllowAnonymous]
        [HttpGet("public/{token}")]
        public async Task<IActionResult> GetByToken(string token, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(token)) return NotFound();

            var shareLink = await _db.ShareLinks
                .AsNoTracking()
                .AsSplitQuery()
                .Include(s => s.UploadCollection).ThenInclude(c => c.Photos)
                .Include(s => s.UploadCollection).ThenInclude(c => c.Archives)
                .SingleOrDefaultAsync(s =>
                    s.Token == token &&
                    !s.IsRevoked &&
                    !s.UploadCollection.IsDeleted,
                    ct);

            if (shareLink is null) return NotFound();
            if (shareLink.ExpiresAtUtc.HasValue && shareLink.ExpiresAtUtc.Value < DateTime.UtcNow) return NotFound();

            var collection = shareLink.UploadCollection;

            var readyPhotos = collection.Photos
                .Where(p => p.Status == PhotoStatus.Ready)
                .OrderBy(p => p.TakenAt ?? p.CreatedAtUtc)
                .ToList();

            var photos = new List<object>();

            double? prevLat = null;
            double? prevLng = null;
            double totalDistance = 0;

            foreach (var p in readyPhotos)
            {
                string? thumbUrl = null;
                string? standardUrl = null;
                string? originalUrl = null;

                if (!string.IsNullOrWhiteSpace(p.ThumbKey))
                {
                    thumbUrl = await _storage.GeneratePresignedDownloadUrlAsync(p.ThumbKey, TimeSpan.FromHours(2));
                }
                if (!string.IsNullOrWhiteSpace(p.StandardKey))
                {
                    standardUrl = await _storage.GeneratePresignedDownloadUrlAsync(p.StandardKey, TimeSpan.FromHours(2));
                }
                if (!string.IsNullOrWhiteSpace(p.OriginalKey))
                {
                    originalUrl = await _storage.GeneratePresignedDownloadUrlAsync(p.OriginalKey, TimeSpan.FromHours(2));
                }

                double? distanceFromPrevious = null;

                if (prevLat.HasValue && prevLng.HasValue && p.Latitude.HasValue && p.Longitude.HasValue)
                {
                    distanceFromPrevious = Calculator.DistanceBetweenLocations(prevLat.Value,prevLng.Value,p.Latitude.Value,p.Longitude.Value);
                    totalDistance += distanceFromPrevious.Value;
                }

                photos.Add(new
                {
                    p.Id,
                    p.OriginalFileName,
                    p.Description,
                    p.Width,
                    p.Height,
                    p.CreatedAtUtc,
                    p.TakenAt,
                    p.Latitude,
                    p.Longitude,
                    Status = p.Status.ToString(),
                    p.Error,
                    ThumbUrl = thumbUrl,
                    StandardUrl = standardUrl,
                    OriginalUrl = originalUrl,
                    DistanceFromPrevious = distanceFromPrevious
                });

                if (p.Latitude.HasValue && p.Longitude.HasValue)
                {
                    prevLat = p.Latitude.Value;
                    prevLng = p.Longitude.Value;
                }
            }
            
            var archives = new List<object>();
            var orderedArchives = collection.Archives.OrderByDescending(a => a.CreatedAtUtc);

            foreach (var a in orderedArchives)
            {
                string? downloadUrl = null;

                if (!string.IsNullOrWhiteSpace(a.StorageKey))
                {
                    downloadUrl = await _storage.GeneratePresignedDownloadUrlAsync(
                        a.StorageKey,
                        TimeSpan.FromMinutes(30),
                        a.OriginalFileName,
                        true);
                }

                archives.Add(new
                {
                    a.Id,
                    a.OriginalFileName,
                    a.Description,
                    a.SizeBytes,
                    a.CreatedAtUtc,
                    DownloadUrl = downloadUrl
                });
            }

            return Ok(new
            {
                ShareLink = new
                {
                    shareLink.Id,
                    shareLink.Token,
                    shareLink.CreatedAtUtc,
                    shareLink.ExpiresAtUtc,
                    shareLink.AllowSlideshowOriginals,
                    shareLink.AllowDownloadResizedZip,
                    shareLink.AllowDownloadOriginalFromCard
                },

                Collection = new
                {
                    collection.Id,
                    collection.Title,
                    collection.Description,
                    collection.CreatedAtUtc,
                    
                    collection.TotalPhotos,
                    collection.TotalArchives,
                    
                    TotalDistance = totalDistance,
                    TotalReadyPhotos = readyPhotos.Count,

                    Photos = photos,
                    Archives = archives
                }
            });
        }

        [AllowAnonymous]
        [HttpGet("public/{token}/download-standard-zip")]
        public async Task<IActionResult> DownloadStandardZipByToekn(
            string token,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(token)) return NotFound();

            var shareLink = await _db.ShareLinks
                .Include(s => s.UploadCollection)
                .SingleOrDefaultAsync(s =>
                    s.Token == token && 
                    !s.IsRevoked &&
                    !s.UploadCollection.IsDeleted,
                    ct);

            if (shareLink is null) return NotFound();
            if (shareLink.ExpiresAtUtc.HasValue && shareLink.ExpiresAtUtc.Value < DateTime.UtcNow) return NotFound();
            if (!shareLink.AllowDownloadResizedZip) return Forbid();

            var collectionId = shareLink.UploadCollectionId;

            try
            {
                var result = await _archiveCollectionService.BuildStandardZipAsync(collectionId, ct);
                _logger.LogInformation(
                    "Shared standard ZIP built. CollectionId={CollectionId}, ShareLinkId={ShareLinkId}, FilesCount={FilesCount}, TotalBytes={TotalBytes}",
                    collectionId,
                    shareLink.Id,
                    result.FilesCount,
                    result.TotalBytes);

                Response.OnCompleted(() =>
                {
                    try
                    {
                        result.Stream.Dispose();

                        if (result.Stream is FileStream fileStream)
                        {
                            var path = fileStream.Name;
                            if (System.IO.File.Exists(path))
                            {
                                System.IO.File.Delete(path);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to cleanup temp shared archive file for CollectionId={CollectionId}",
                            collectionId);
                    }

                    return Task.CompletedTask;
                });

                return File(result.Stream, result.ContentType, result.FileName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to build shared standard ZIP for CollectionId={CollectionId}",
                    collectionId);

                return StatusCode(500, "Failed to create ZIP archive");
            }
        }

        //-------------------------------------------------------
        // Progress bar for ZIP archive download
        //-------------------------------------------------------

        [AllowAnonymous]
        [HttpPost("public/{token}/standard-zip-jobs")]
        public async Task<IActionResult> StartSharedStandardZipJob(
            string token,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(token)) return NotFound();

            var shareLink = await _db.ShareLinks
                .AsNoTracking()
                .Include(s => s.UploadCollection)
                .SingleOrDefaultAsync(s =>
                    s.Token == token &&
                    !s.IsRevoked &&
                    !s.UploadCollection.IsDeleted, 
                    ct);

            if (shareLink == null) return NotFound();
            if (shareLink.ExpiresAtUtc.HasValue && shareLink.ExpiresAtUtc.Value < DateTime.UtcNow) return NotFound();
            if(!shareLink.AllowDownloadResizedZip) return Forbid();

            var collectionId = shareLink.UploadCollectionId;

            var totalFiles = await _db.PhotoItems
                .CountAsync(p =>
                    p.UploadCollectionId == collectionId &&
                    p.Status == PhotoStatus.Ready &&
                    !string.IsNullOrWhiteSpace(p.StandardKey),
                    ct);

            if (totalFiles == 0) return BadRequest("No resized photos available for ZIP archive.");

            var jobId = _zipJobStore.Create(totalFiles);

            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await _archiveCollectionService.BuildStandardZipAsync(
                        collectionId,
                        jobId,
                        _zipJobStore,
                        CancellationToken.None);

                    if (result.Stream is FileStream fileStream)
                    {
                        _zipJobStore.MarkReady(
                            jobId,
                            fileStream.Name,
                            result.FileName,
                            result.ContentType);
                    }
                    else
                    {
                        _zipJobStore.MarkFailed(jobId, "ZIP stream is not a file stream.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Shared standard ZIP job failed. JobId={JobId}, CollectionId={CollectionId}, ShareLinkId={ShareLinkId}",
                        jobId,
                        collectionId,
                        shareLink.Id);

                    _zipJobStore.MarkFailed(jobId, ex.Message);
                }
            });

            return Ok(new { jobId });
        }

        [AllowAnonymous]
        [HttpGet("public/standard-zip-jobs/{jobId:guid}/status")]
        public IActionResult GetSharedStandardZipJobStatus(Guid jobId)
        {
            var job = _zipJobStore.Get(jobId);

            if (job is null) return NotFound();

            return Ok(new 
            { 
                job.JobId,
                Status = job.Status.ToString(),
                job.ProcessedFiles,
                job.TotalFiles,
                job.Percent,
                job.Error
            });
        }

        [AllowAnonymous]
        [HttpGet("public/standard-zip-jobs/{jobId:guid}/download")]
        public IActionResult DownloadSharedStandardZipJob(Guid jobId)
        {
            var job = _zipJobStore.Get(jobId);

            if (job is null) return NotFound();
            if (job.Status != ZipJobStatus.Ready) return BadRequest("ZIP archive is not ready yet.");
            if (string.IsNullOrWhiteSpace(job.FilePath) || !System.IO.File.Exists(job.FilePath)) return NotFound("ZIP file not found.");

            var stream = new FileStream(
                job.FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            Response.OnCompleted(() =>
            {
                try
                {
                    stream.Dispose();

                    if (System.IO.File.Exists(job.FilePath))
                    {
                        System.IO.File.Delete(job.FilePath);
                    }

                    _zipJobStore.Remove(jobId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to cleanup shared ZIP job file. JobId={JobId}",
                        jobId);
                }

                return Task.CompletedTask;
            });

            return File(stream, job.ContentType, job.FileName);
        }





        //-------------------------------------------------------
        // Helper
        //-------------------------------------------------------
        private string? GetUserId()
        {
            return User.FindFirstValue("sub")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.Identity?.Name;
        }

        private string BuildShareLink(string token)
        {
            var frontendBaseUrl = _configuration["Frontend:BaseUrl"]?.TrimEnd('/');

            //return $"{frontendBaseUrl}://{Request.Host}/shared/{token}";
            return $"{frontendBaseUrl}/shared/{token}";
        }
    }
}
