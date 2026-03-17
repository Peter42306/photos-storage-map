using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Domain.Entities;
using PhotosStorageMap.Infrastructure.Data;
using System.Security.Claims;
using PhotosStorageMap.Domain.Enums;

namespace PhotosStorageMap.Api.Controllers
{
    [Route("api/collections")]
    [ApiController]
    [Authorize]
    public sealed class CollectionsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileStorage _storage;
        private readonly ILogger<CollectionsController> _logger;
        private readonly IArchiveCollectionService _archiveCollectionService;

        public CollectionsController(
            ApplicationDbContext db,
            IFileStorage storage,
            ILogger<CollectionsController> logger,
            IArchiveCollectionService archiveCollectionService)
        {
            _db = db;
            _storage = storage;
            _logger = logger;            
            _archiveCollectionService = archiveCollectionService;
        }


        [HttpGet]
        public async Task<ActionResult> GetMyCollections(CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var collections = await _db.UploadCollections
                .Where(x => x.OwnerUserId == userId && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Description,
                    x.CreatedAtUtc,
                    //x.ExpiresAtUtc,
                    x.TotalPhotos,
                    x.TotalBytes

                })
                .ToListAsync(ct);

            return Ok(collections);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult> GetMyCollectionById(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var collection = await _db.UploadCollections
                .AsNoTracking()
                .Where(c => c.Id == id && c.OwnerUserId == userId && !c.IsDeleted) 
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.CreatedAtUtc,
                    c.TotalPhotos,
                    c.TotalBytes,
                    Photos = c.Photos
                        .Where(p => p.Status == PhotoStatus.Ready)
                        .OrderBy(p => p.TakenAt ?? p.CreatedAtUtc) // TODO add filter by status.ready
                        .Select(p => new
                        {
                            p.Id,
                            p.OriginalFileName,
                            p.Description,
                            p.ThumbKey,
                            p.StandardKey,
                            p.Width,
                            p.Height,
                            p.TotalSizeBytes,
                            p.ThumbSizeBytes,
                            p.StandardSizeBytes,
                            p.OriginalSizeBytes,
                            p.CreatedAtUtc,
                            p.TakenAt,
                            p.Latitude,
                            p.Longitude,
                            Status = p.Status.ToString(),
                            p.Error
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);

            if (collection is null) return NotFound();

            var photos = new List<object>();
            foreach ( var p in collection.Photos)
            {
                string? thumbUrl = null;

                if (!string.IsNullOrWhiteSpace(p.ThumbKey))
                {
                    thumbUrl = await _storage.GeneratePresignedDownloadUrlAsync(p.ThumbKey, TimeSpan.FromHours(2));
                }                

                photos.Add(new
                {
                    p.Id,
                    p.OriginalFileName,
                    p.Description,                    
                    p.Width,
                    p.Height,
                    p.TotalSizeBytes,
                    p.ThumbSizeBytes,
                    p.StandardSizeBytes,
                    p.OriginalSizeBytes,
                    p.CreatedAtUtc,
                    p.TakenAt,
                    p.Latitude,
                    p.Longitude,
                    p.Status,
                    p.Error,
                    ThumbUrl = thumbUrl
                });
            }
            
            return Ok(new
            {
                collection.Id,
                collection.Title,
                collection.Description,
                collection.CreatedAtUtc,
                collection.TotalPhotos,
                collection.TotalBytes,
                Photos = photos
            });
        }

        // update collection title & description
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateCollection(Guid id, [FromBody] UpdateCollectionRequest request, CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var collection = await _db.UploadCollections
                .FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == userId && !x.IsDeleted);

            if (collection is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(request.Title)) 
            {
                collection.Title = request.Title.Trim(); 
            }

            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                collection.Description = request.Description.Trim();
            }
            
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // create empty collection
        [HttpPost]
        public async Task<ActionResult<Guid>> CreateCollection(CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var title = $"Collection {DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}";

            var collection = new UploadCollection
            {
                OwnerUserId = userId,
                Title = title,                
                CreatedAtUtc = DateTime.UtcNow,
            };

            _db.UploadCollections.Add(collection);
            await _db.SaveChangesAsync(ct);

            return Ok(collection.Id);
        }
        

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteCollection(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var collection = await _db.UploadCollections
                .Include(c => c.Photos)
                .FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == userId && !x.IsDeleted, ct);

            if (collection is null) return NotFound();

            // 1) S3 cleanup
            //foreach (var photo in collection.Photos)
            //{
            //    try
            //    {
            //        if (!string.IsNullOrWhiteSpace(photo.OriginalKey))
            //        {
            //            await _storage.DeleteAsync(photo.OriginalKey, ct);
            //            _logger.LogInformation("DELETE COLLECTION: DELETE PHOTO from S3 for photoId={PhotoId}, StorageKey={StorageKey}", photo.Id, photo.OriginalKey);
            //        }

            //        if (!string.IsNullOrWhiteSpace(photo.StandardKey))
            //        {
            //            await _storage.DeleteAsync(photo.StandardKey, ct);
            //            _logger.LogInformation("DELETE COLLECTION: DELETE PHOTO from S3 for photoId={PhotoId}, StorageKey={StorageKey}", photo.Id, photo.StandardKey);
            //        }

            //        if (!string.IsNullOrWhiteSpace(photo.ThumbKey))
            //        {
            //            await _storage.DeleteAsync(photo.ThumbKey, ct);
            //            _logger.LogInformation("DELETE COLLECTION: DELETE PHOTO from S3 for photoId={PhotoId}, StorageKey={StorageKey}", photo.Id, photo.ThumbKey);
            //        }

            //        _logger.LogInformation("DELETE COLLECTION: DELETE PHOTO from S3 for photoId={PhotoId}", photo.Id);
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogWarning(ex, "DELETE COLLECTION: failed to delete S3 object for photoId={PhotoId}", photo.Id);
            //    }
            //}

            // 2) DB delete
            //_db.UploadCollections.Remove(collection);

            collection.IsDeleted = true;            
            await _db.SaveChangesAsync(ct);

            //_logger.LogInformation("DELETE COLLECTION: deleted collectionId={CollectionId}", id);
            _logger.LogInformation("DELETE COLLECTION: masked as deleted collectionId={CollectionId}", id);

            return NoContent();
        }

        [HttpGet("{id:guid}/map")]
        public async Task<ActionResult> GetCollectionMap(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var collectionExists = await _db.UploadCollections
                .AnyAsync(c => c.Id == id && c.OwnerUserId == userId && !c.IsDeleted, ct);

            if (!collectionExists) return NotFound();
            
            var photos = await _db.PhotoItems
                .Where(p => 
                    p.UploadCollectionId == id && 
                    p.Status == Domain.Enums.PhotoStatus.Ready && 
                    p.Latitude.HasValue && 
                    p.Longitude.HasValue)
                .OrderBy(p => p.TakenAt ?? p.CreatedAtUtc)
                .Select(p => new
                {
                    id = p.Id,
                    originalFileName = p.OriginalFileName,
                    latitude = p.Latitude,
                    longitude = p.Longitude,
                    takenAt = p.TakenAt,
                    thumbKey = p.ThumbKey
                })
                .ToListAsync(ct);

            var result = new List<object>();

            foreach (var photo in photos)
            {
                string? thumbUrl = null;

                if (!string.IsNullOrWhiteSpace(photo.thumbKey))
                {
                    thumbUrl = await _storage.GeneratePresignedDownloadUrlAsync(photo.thumbKey, TimeSpan.FromMinutes(30));
                }

                result.Add(new
                {
                    photo.id,
                    photo.originalFileName,
                    photo.latitude,
                    photo.longitude,
                    photo.takenAt,
                    thumbUrl
                });
            }

            return Ok(result);
        }

        // download standard side photos to zip
        [HttpGet("{id:guid}/download-standard-zip")]
        public async Task<IActionResult> DownloadStandardZip(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var collectionExists = await _db.UploadCollections
                .AnyAsync(c => c.Id == id && c.OwnerUserId == userId && !c.IsDeleted, ct);

            if (!collectionExists) return NotFound();

            try
            {
                var result = await _archiveCollectionService.BuildStandardZipAsync(id, ct);
                
                _logger.LogInformation(
                    "Standard ZIP built. CollectionId={CollectionId}, FilesCount={FilesCount}, TotalBytes={TotalBytes}", id,
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
                        _logger.LogWarning(ex, "Failed to cleanup temp archive file for CollectionId={CollectionId}", id);
                    }

                    return Task.CompletedTask;
                });

                return File(result.Stream, result.ContentType, result.FileName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to build standard ZIP for CollectionId={CollectionId}", id);
                return StatusCode(500, "Failed to create ZIP archive");
            }
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
    }
}
