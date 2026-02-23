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

        //[HttpPost("collection")]
        //public async Task<ActionResult<Guid>> CreateCollection(CancellationToken ct)
        //{
        //    var userId = GetUserId();
        //    if(string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        //    var collection = new UploadCollection
        //    {
        //        OwnerUserId = userId,
        //        CreatedAtUtc = DateTime.UtcNow
        //    };

        //    _db.UploadCollections.Add(collection);
        //    await _db.SaveChangesAsync();

        //    return Ok(collection.Id);
        //}

        [HttpPost("init")]
        public async Task<ActionResult> InitUpload([FromQuery] Guid collectionId, CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var collection = await _db.UploadCollections.FindAsync(new object[] { collectionId }, ct);
            if (collection is null) return NotFound();
            if (collection.OwnerUserId != userId) return Forbid();

            var photo = new PhotoItem
            {
                UploadCollectionId = collectionId,
                Status = PhotoStatus.Uploaded,
                CreatedAtUtc = DateTime.UtcNow,
            };

            var storageKey = StorageKeys.Original(userId, collectionId, photo.Id);

            var uploadUrl = await _fileStorage.GeneratePresignedUploadUrlAsync(storageKey, TimeSpan.FromMinutes(5));

            photo.OriginalKey = storageKey;

            _db.PhotoItems.Add(photo);
            await _db.SaveChangesAsync(ct);

            return Ok(new
            {
                photoId = photo.Id,
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
