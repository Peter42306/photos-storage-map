using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Domain.Entities;
using PhotosStorageMap.Infrastructure.Data;
using System.Security.Claims;
using PhotosStorageMap.Domain.Enums;
using PhotosStorageMap.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace PhotosStorageMap.Api.Controllers
{
    [Route("api/uploads")]
    [ApiController] 
    [Authorize]
    public sealed class UploadsController : ControllerBase
    {
        private readonly IFileStorage _fileStorage;
        private readonly IPhotoProcessingQueue _queue;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UploadsController> _logger;

        public UploadsController(
            IFileStorage fileStorage,
            IPhotoProcessingQueue queue,
            ApplicationDbContext db,
            ILogger<UploadsController> logger)
        {
            _fileStorage = fileStorage;
            _queue = queue;
            _db = db;
            _logger = logger;
        }

        [HttpPost("collection")]
        public async Task<ActionResult<Guid>> CreateCollection(CancellationToken ct)
        {
            var userId = GetUserId();
            if(string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var collection = new UploadCollection
            {
                OwnerUserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.UploadCollections.Add(collection);
            await _db.SaveChangesAsync(ct);

            return Ok(collection.Id);
        }

        [HttpPost("init")]
        public async Task<ActionResult> InitUpload(
            [FromQuery] Guid collectionId, 
            [FromQuery] string? fileName,
            [FromQuery] long? fileSize,
            CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var collection = await _db.UploadCollections.FindAsync(new object[] { collectionId }, ct);
            if (collection is null) return NotFound();
            if (collection.OwnerUserId != userId) return Forbid();

            var safeName = string.IsNullOrWhiteSpace(fileName) ? $"photo_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg" : fileName.Trim();

            var photoId = Guid.NewGuid();
            var storageKey = StorageKeys.Original(userId, collectionId, photoId);
            var uploadUrl = await _fileStorage.GeneratePresignedUploadUrlAsync(storageKey, TimeSpan.FromMinutes(50));

            var photo = new PhotoItem
            {
                Id = photoId,
                UploadCollectionId = collectionId,
                OriginalFileName = safeName,
                OriginalKey = storageKey,
                OriginalSizeBytes = (fileSize.HasValue && fileSize.Value > 0) ? fileSize.Value : null,
                Status = PhotoStatus.Uploaded,
                CreatedAtUtc = DateTime.UtcNow,
            };

            _db.PhotoItems.Add(photo);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("UPLOAD: InitUpload photoId={PhotoId}", photoId);

            return Ok(new
            {
                photoId,
                uploadUrl
            });
        }

        [HttpPost("{photoId:guid}/complete")]
        public async Task<IActionResult> CompleteUpload(Guid photoId, CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var photo = await _db.PhotoItems                
                .Include(p => p.UploadCollection)
                .FirstOrDefaultAsync(p => p.Id == photoId && p.UploadCollection.OwnerUserId == userId, ct);

            if (photo == null) return NotFound($"Photo {photoId} not found");

            photo.Status = PhotoStatus.Processing;

            await _db.SaveChangesAsync(ct);
            await _queue.EnqueueAsync(photoId, ct);

            _logger.LogInformation("UPLOAD: CompleteUpload photoId={PhotoId}", photoId);

            return Ok();
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
