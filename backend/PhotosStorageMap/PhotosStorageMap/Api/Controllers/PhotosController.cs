using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Domain.Entities;
using PhotosStorageMap.Infrastructure.Data;
using System.Security.Claims;

namespace PhotosStorageMap.Api.Controllers
{
    [Route("api/photos")]
    [ApiController]
    [Authorize]
    public class PhotosController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileStorage _storage;
        private readonly ILogger<PhotosController> _logger;

        public PhotosController(
            ApplicationDbContext db,
            IFileStorage storage,
            ILogger<PhotosController> logger)
        {
            _db = db;
            _storage = storage;
            _logger = logger;
        }

        [HttpGet("{photoId:guid}/thumb-url")]
        public async Task<ActionResult<string>> GetThumbUrl(Guid photoId, CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var photo = await LoadOwnedPhoto(userId, photoId, ct);

            //if (string.IsNullOrWhiteSpace(photo.ThumbKey))
            //{
            //    return Conflict(new { message = "Thumbnail is not ready yet", status = photo.Status });
            //}

            if (photo is null) return NotFound();

            if (photo.Status != Domain.Enums.PhotoStatus.Ready || string.IsNullOrWhiteSpace(photo.ThumbKey))
            {
                return Ok(new 
                {
                    status = photo.Status.ToString(), 
                    url = (string?)null 
                });
            }

            var url = await _storage.GeneratePresignedDownloadUrlAsync(photo.ThumbKey, TimeSpan.FromMinutes(10));
            return Ok(new
            {
                status = photo.Status.ToString(),
                url,
            });
        }

        [HttpGet("{photoId:guid}/standard-url")]
        public async Task<ActionResult<string>> GetStandardUrl(Guid photoId, CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var photo = await LoadOwnedPhoto(userId, photoId, ct);

            if (photo is null) return NotFound();

            if (photo.Status != Domain.Enums.PhotoStatus.Ready || string.IsNullOrWhiteSpace(photo.StandardKey))
            {
                return Ok(new { status = photo.Status.ToString(), url = (string?)null });
            }

            var url = await _storage.GeneratePresignedDownloadUrlAsync(photo.StandardKey, TimeSpan.FromMinutes(10));
            return Ok(url);
        }

        [HttpDelete("{photoId:guid}")]
        public async Task<IActionResult> DeletePhoto(Guid photoId, CancellationToken ct)
        {
            var userId = GetUserId();
            if(string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var photo = await _db.PhotoItems
                .Include(p => p.UploadCollection)
                .FirstOrDefaultAsync(p => p.Id == photoId, ct);

            if (photo is null) return NotFound();

            if(photo.UploadCollection?.OwnerUserId != userId) return NotFound();

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
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DELETE PHOTO: Failed to delete storage objects for photoId={PhotoId}", photoId);
            }

            if (photo.UploadCollection is not null)
            {
                if (photo.TotalSizeBytes.HasValue)
                {
                    photo.UploadCollection.TotalBytes = Math.Max(0, photo.UploadCollection.TotalBytes - photo.TotalSizeBytes.Value);
                }

                photo.UploadCollection.TotalPhotos = Math.Max(0, photo.UploadCollection.TotalPhotos - 1);
            }

            _db.PhotoItems.Remove(photo);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("DELETE PHOTO: Successfully deleted photo, photoId={PhotoId}", photoId);

            return NoContent();
        }

        [HttpGet("{photoId:guid}/status")]
        public async Task<ActionResult> GetStatus(Guid photoId, CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var photo = await _db.PhotoItems
                .Include(p => p.UploadCollection)
                .Where(p => p.Id == photoId && p.UploadCollection.OwnerUserId == userId)
                .Select(p => new
                {
                    status = p.Status.ToString(),
                    thumbkey = p.ThumbKey,
                    standardkey = p.StandardKey,
                    error = p.Error
                })
                .FirstOrDefaultAsync(ct);

            if (photo is null) return NotFound();
            return Ok(photo);
        }

        // endpoint to view original in browser
        [HttpGet("{photoId:guid}/original-url")]
        public async Task<ActionResult> GetOriginalUrl(Guid photoId, CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var photo = await _db.PhotoItems
                .Include(p => p.UploadCollection)
                .FirstOrDefaultAsync(p => p.Id == photoId && p.UploadCollection.OwnerUserId == userId);

            if (photo is null) return NotFound();
            if (string.IsNullOrWhiteSpace(photo.OriginalKey)) return NotFound();

            var url = await _storage.GeneratePresignedDownloadUrlAsync(
                photo.OriginalKey, 
                TimeSpan.FromMinutes(30));

            return Ok(new { url });
        }

        // endpoint to download original
        [HttpGet("{photoId:guid}/original-download-url")]
        public async Task<ActionResult> GetOriginalDownloadUrl(Guid photoId, CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var photo = await _db.PhotoItems
                .Include(p => p.UploadCollection)
                .FirstOrDefaultAsync(p => p.Id == photoId && p.UploadCollection.OwnerUserId == userId, ct);

            if (photo is null) return NotFound();
            if (string.IsNullOrWhiteSpace(photo.OriginalKey)) return NotFound();

            var fileName = string.IsNullOrWhiteSpace(photo.OriginalFileName) 
                ? $"photo_{photo.Id}.jpg" 
                : photo.OriginalFileName;

            var url = await _storage.GeneratePresignedDownloadUrlAsync(
                storageKey: photo.OriginalKey, 
                expiresIn: TimeSpan.FromMinutes(30),
                downloadFileName: fileName,
                forceDownload: true);

            return Ok(new { url });
        }

        // photo description
        [HttpPut("{photoId:guid}/description")]
        public async Task<ActionResult> UpdateDescription(
            Guid photoId, 
            [FromBody] UpdatePhotoDescriptionRequest request,
            CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var photo = await _db.PhotoItems
                .Include(p => p.UploadCollection)
                .FirstOrDefaultAsync(p => p.Id == photoId && p.UploadCollection.OwnerUserId == userId, ct);

            if (photo is null) return NotFound();

            photo.Description = string.IsNullOrWhiteSpace(request.Description) 
                ? null
                : request.Description.Trim();

            await _db.SaveChangesAsync(ct);

            return Ok( new
            {
                message = "Photo description updated.",
                description = photo.Description
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

        private async Task<PhotoItem?> LoadOwnedPhoto(string userId, Guid photoId, CancellationToken ct)
        {
            var photo = await _db.PhotoItems
                .Include(p => p.UploadCollection)
                .FirstOrDefaultAsync(p => p.Id == photoId, ct);

            if (photo is null) return null;
            if (photo.UploadCollection.OwnerUserId != userId) return null;

            return photo;
        }
    }
}
