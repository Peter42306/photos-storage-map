using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Domain.Entities;
using PhotosStorageMap.Infrastructure.Data;
using System.Security.Claims;

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

        public CollectionsController(
            ApplicationDbContext db,
            IFileStorage storage,
            ILogger<CollectionsController> logger)
        {
            _db = db;
            _storage = storage;
            _logger = logger;
        }


        [HttpGet]
        public async Task<ActionResult> GetMyCollections(CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var collections = await _db.UploadCollections
                .Where(x => x.OwnerUserId == userId)
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
                .Where(c => c.Id == id && c.OwnerUserId == userId)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.CreatedAtUtc,
                    c.TotalPhotos,
                    c.TotalBytes,
                    Photos = c.Photos
                        .OrderByDescending(p => p.CreatedAtUtc)
                        .Select(p => new
                        {
                            p.Id,
                            p.OriginalFileName,
                            p.ThumbKey,
                            p.StandardKey,
                            p.Width,
                            p.Height,
                            p.SizeBytes,
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
            
            return Ok(collection);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateCollection(Guid id, [FromBody] UpdateCollectionRequest request, CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var collection = await _db.UploadCollections
                .FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == userId);

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
                .FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == userId, ct);

            if (collection is null) return NotFound();

            // 1) S3 cleanup
            foreach (var photo in collection.Photos)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(photo.OriginalKey))
                    {
                        await _storage.DeleteAsync(photo.OriginalKey, ct);
                    }

                    if (!string.IsNullOrWhiteSpace(photo.StandardKey))
                    {
                        await _storage.DeleteAsync(photo.StandardKey, ct);
                    }

                    if (!string.IsNullOrWhiteSpace(photo.ThumbKey))
                    {
                        await _storage.DeleteAsync(photo.ThumbKey, ct);
                    }

                    _logger.LogInformation("DELETE COLLECTION: DELETE PHOTO from S3 for photoId={PhotoId}", photo.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "DELETE COLLECTION: failed to delete S3 object for photoId={PhotoId}", photo.Id);
                }
            }

            // 2) DB delete
            _db.UploadCollections.Remove(collection);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("DELETE COLLECTION: deleted collectionId={CollectionId}", id);
            return NoContent();
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
