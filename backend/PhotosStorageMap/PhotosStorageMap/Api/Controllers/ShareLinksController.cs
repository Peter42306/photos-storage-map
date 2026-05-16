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
        private readonly ILogger<ShareLinksController> _logger;
        
        public ShareLinksController(
            ApplicationDbContext db,
            IFileStorage storage,
            IConfiguration configuration,
            ILogger<ShareLinksController> logger)
        {
            _db = db;
            _storage = storage;
            _configuration = configuration;
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
                .Include(c => c.ShareLink)
                .FirstOrDefaultAsync(c => 
                c.Id == request.CollectionId &&
                c.OwnerUserId == userId &&
                !c.IsDeleted,
                ct);

            if (collection == null) return NotFound("Collection not found.");

            var token = Guid.NewGuid().ToString("N");

            if (collection.ShareLink is null)
            {
                collection.ShareLink = new ShareLink
                {
                    UploadCollectionId = collection.Id,
                    Token = token,
                    CreatedAtUtc = DateTime.UtcNow,
                    IsRevoked = false,
                    AllowSlideshowOriginals = request.AllowSlideshowOriginals,
                    AllowDownloadResizedZip = request.AllowDownloadResizedZip,
                    AllowDownloadOriginalFromCard = request.AllowDownloadOriginalFromCard
                };
            }
            else 
            {
                collection.ShareLink.Token = token;
                collection.ShareLink.CreatedAtUtc = DateTime.UtcNow;
                collection.ShareLink.IsRevoked = false;
                collection.ShareLink.AllowSlideshowOriginals = request.AllowSlideshowOriginals;
                collection.ShareLink.AllowDownloadResizedZip = request.AllowDownloadResizedZip;
                collection.ShareLink.AllowDownloadOriginalFromCard = request.AllowDownloadOriginalFromCard;
            }

            await _db.SaveChangesAsync(ct);

            var shareLink = collection.ShareLink;
            var url = BuildShareLink(shareLink.Token);
            //var url = $"{Request.Scheme}://{Request.Host}/shared/{shareLink.Token}";

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
            var frontendBaseUrl = _configuration["Frontend:BaseUrl"];

            return $"{frontendBaseUrl}://{Request.Host}/shared/{token}";
        }
    }
}
