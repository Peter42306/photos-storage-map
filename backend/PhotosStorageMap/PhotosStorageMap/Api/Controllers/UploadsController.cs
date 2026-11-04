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
using Npgsql;

namespace PhotosStorageMap.Api.Controllers
{
    [Route("api/uploads")]
    [ApiController] 
    [Authorize]
    public sealed class UploadsController : ControllerBase
    {
        private readonly IFileStorage _fileStorage;
        private readonly IPhotoProcessingQueue _queue;
        private readonly ICollectionStatsService _statsService;
        private readonly IStorageLimitService _storageLimitService;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UploadsController> _logger;

        public UploadsController(
            IFileStorage fileStorage,
            IPhotoProcessingQueue queue,
            ICollectionStatsService statsService,
            IStorageLimitService storageLimitService,
            ApplicationDbContext db,
            ILogger<UploadsController> logger)
        {
            _fileStorage = fileStorage;
            _queue = queue;
            _statsService = statsService;
            _storageLimitService = storageLimitService;
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
            [FromQuery] Guid? clientUploadId,
            CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var collection = await _db.UploadCollections.FindAsync(new object[] { collectionId }, ct);
            if (collection is null) return NotFound();
            if (collection.OwnerUserId != userId) return Forbid();

            if (clientUploadId.HasValue)
            {
                if (clientUploadId.Value == Guid.Empty)
                {
                    return BadRequest("Invalid client upload ID.");
                }

                var existingPhoto = await _db.PhotoItems
                    .FirstOrDefaultAsync(p => p.ClientUploadId == clientUploadId.Value,
                    ct);

                if (existingPhoto is not null)
                {
                    if (existingPhoto.UploadCollectionId != collectionId)
                    {
                        return Conflict("Client upload ID belongs to another collection.");
                    }

                    if (existingPhoto.OriginalFileName != fileName?.Trim() || existingPhoto.OriginalSizeBytes != fileSize)
                    {
                        return Conflict("Client upload ID belongs to another file.");
                    }

                    if (existingPhoto.Status != PhotoStatus.Uploading)
                    {
                        return Conflict($"Upload is already in status '{existingPhoto.Status}'.");
                    }

                    if (string.IsNullOrWhiteSpace(existingPhoto.OriginalKey))
                    {
                        return Conflict("Existing storage key is missing.");
                    }

                    var existingUploadUrl = await _fileStorage.GeneratePresignedUploadUrlAsync(
                        existingPhoto.OriginalKey!, 
                        TimeSpan.FromMinutes(50));

                    _logger.LogInformation(
                        "UPLOAD: InitUpload reused existing photoId={PhotoId}, clientUploadId={ClientUploadId}",
                        existingPhoto.Id,
                        clientUploadId);

                    return Ok(new
                    {
                        photoId = existingPhoto.Id,
                        uploadUrl = existingUploadUrl
                    });
                }
            }


            // 1) actual totals from DB
            var actualStats = await _statsService.SyncStoredStatsAsync(collectionId, ct);
                        
            // 2) validate plan limits
            var limits = await _storageLimitService.GetUserLimitsAsync(userId, ct);

            if (!limits.Success) return Unauthorized();            

            if (actualStats.TotalPhotos >= limits.MaxPhotosPerCollection)
            {
                return BadRequest($"Collection photo limit reached. Maximum photos per collection for your plan: {limits.MaxPhotosPerCollection}.");
            }

            if (fileSize.HasValue && fileSize.Value > 0)
            {
                // check if user can upload, if have enough space by plan
                var storageCheck = await _storageLimitService.CanAddBytesAsync(
                    userId, 
                    fileSize.Value,
                    ct);

                if (!storageCheck.Allowed)
                {
                    return BadRequest(storageCheck.Message);
                }
            }            
            
            var safeName = string.IsNullOrWhiteSpace(fileName) 
                ? $"photo_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg"
                : fileName.Trim();

            var photoId = Guid.NewGuid();
            var storageKey = StorageKeys.Original(userId, collectionId, photoId);
            var uploadUrl = await _fileStorage.GeneratePresignedUploadUrlAsync(storageKey, TimeSpan.FromMinutes(50));

            var photo = new PhotoItem
            {
                Id = photoId,
                ClientUploadId = clientUploadId,
                UploadCollectionId = collectionId,
                OriginalFileName = safeName,
                OriginalKey = storageKey,
                OriginalSizeBytes = (fileSize.HasValue && fileSize.Value > 0) ? fileSize.Value : null,
                Status = PhotoStatus.Uploading,
                CreatedAtUtc = DateTime.UtcNow,
            };

            _db.PhotoItems.Add(photo);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (
                clientUploadId.HasValue &&
                ex.InnerException is PostgresException pgEx &&
                pgEx.SqlState == PostgresErrorCodes.UniqueViolation &&
                pgEx.ConstraintName == "IX_PhotoItems_ClientUploadId")
            {
                _db.Entry(photo).State = EntityState.Detached;

                var existingPhoto = await _db.PhotoItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ClientUploadId == clientUploadId.Value, ct);

                if (existingPhoto is null)
                {
                    throw; // This should not happen, but just in case
                }

                if (existingPhoto.UploadCollectionId != collectionId)
                {
                    return Conflict("Client upload ID belongs to another collection.");
                }

                if (existingPhoto.OriginalFileName != fileName?.Trim() || existingPhoto.OriginalSizeBytes != fileSize)
                {
                    return Conflict("Client upload ID belongs to another file.");
                }

                if (existingPhoto.Status != PhotoStatus.Uploading)
                {
                    return Conflict($"Upload is already in status '{existingPhoto.Status}'.");
                }

                if (string.IsNullOrWhiteSpace(existingPhoto.OriginalKey))
                {
                    return Conflict("Existing storage key is missing.");
                }

                var existingUploadUrl = await _fileStorage.GeneratePresignedUploadUrlAsync(
                    existingPhoto.OriginalKey!,
                    TimeSpan.FromMinutes(50));

                _logger.LogWarning(
                    "UPLOAD: Concurrent InitUpload detected. Reusing photoId={PhotoId}, clientUploadId={ClientUploadId}",
                    existingPhoto.Id,
                    clientUploadId);

                return Ok(new
                {
                    photoId = existingPhoto.Id,
                    uploadUrl = existingUploadUrl
                });
            }
            

            _logger.LogInformation("UPLOAD: InitUpload photoId={PhotoId}", photoId);

            return Ok(new
            {
                photoId,
                uploadUrl
            });
        }

        // uploading to S3 confirmation
        [HttpPost("{photoId:guid}/complete")]
        public async Task<IActionResult> CompleteUpload(Guid photoId, CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var photo = await _db.PhotoItems                
                .Include(p => p.UploadCollection)
                .FirstOrDefaultAsync(p => p.Id == photoId && p.UploadCollection.OwnerUserId == userId, ct);

            if (photo is null) return NotFound($"Photo {photoId} not found");
            if (photo.Status == PhotoStatus.Ready) return Ok(); 
            if (photo.Status == PhotoStatus.Processing) return Ok();
            if (photo.Status != PhotoStatus.Uploading) return Conflict($"Cannot complete upload for photo with status {photo.Status}");
            if (string.IsNullOrWhiteSpace(photo.OriginalKey)) return BadRequest("OriginalKey in missing");

            try
            {
                await using var stream = await _fileStorage.OpenReadAsync(photo.OriginalKey, ct);
            }
            catch(OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "UPLOADS : Failed to varify original file in storage. PhotoId={PhotoId}.", 
                    photoId);

                return StatusCode(503, "Failed to verify original file in storage.");
            }            

            var updated = await _db.PhotoItems
                .Where(p => p.Id == photoId && p.Status == PhotoStatus.Uploading)
                .ExecuteUpdateAsync(p => p.SetProperty(x => x.Status, PhotoStatus.Processing), ct);

            if (updated == 0)
            {
                var currentStatus = await _db.PhotoItems
                    .AsNoTracking()
                    .Where(p => p.Id == photoId)
                    .Select(p => p.Status)
                    .FirstOrDefaultAsync(ct);

                if (currentStatus == PhotoStatus.Processing || currentStatus == PhotoStatus.Ready)
                {
                    return Ok(); // already processing or ready, no need to enqueue again
                }

                return Conflict($"Cannot complete upload for photo with status {currentStatus}");
            }

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
