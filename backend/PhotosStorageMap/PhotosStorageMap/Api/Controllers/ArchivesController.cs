using Amazon.Runtime.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhotosStorageMap.Application.Common;
using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Domain.Entities;
using PhotosStorageMap.Infrastructure.Data;
using System.Diagnostics;
using System.Security.Claims;

namespace PhotosStorageMap.Api.Controllers
{
    [Route("api/archives")]
    [ApiController]
    [Authorize]
    public class ArchivesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileStorage _storage;
        private readonly ILogger<ArchivesController> _logger;

        public ArchivesController(
            ApplicationDbContext db,
            IFileStorage storage,
            ILogger<ArchivesController> logger)
        {
            _db = db;
            _storage = storage;
            _logger = logger;
        }



        [HttpPost("init")]
        public async Task<IActionResult> InitUpload(
            [FromBody] InitArchiveUploadRequest request, 
            CancellationToken ct) 
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("ARCHIVES CONTROLLER: InitUpload started, ArchiveName: {ArchiveName}.",request.FileName);

            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            if (request.CollectionId == Guid.Empty) return BadRequest("CollectionId is required.");
            if (string.IsNullOrWhiteSpace(request.FileName)) return BadRequest("FileName is required.");
            if (request.FileSize <= 0) return BadRequest("FileSize must be greater than zero.");
            if (request.FileSize > Limits.ArchiveItem.MaxSizeBytes) return BadRequest($"Archive size exceeds limit of {Limits.ArchiveItem.MaxSizeBytes} bytes.");
            
            var extension = Path.GetExtension(request.FileName)?.ToLowerInvariant();
            if (extension != ContentType.Zip) return BadRequest("Only .zip archive are allowed.");

            var collection = await _db.UploadCollections
                .FirstOrDefaultAsync(
                    c => c.Id == request.CollectionId &&
                    c.OwnerUserId == userId &&
                    !c.IsDeleted,
                    ct);

            if (collection is null) return NotFound("Collection not found.");

            var archiveId = Guid.NewGuid();
            var storageKey = StorageKeys.Archive(userId, request.CollectionId, archiveId, extension);

            var uploadUrl = await _storage.GeneratePresignedUploadUrlAsync(
                storageKey,TimeSpan.FromHours(Limits.ArchiveItem.InitUpload.UrlExpiresIn));

            sw.Stop();
            _logger.LogInformation("ARCHIVES CONTROLLER: InitUpload completed, Duration: {Seconds} ms",
                sw.Elapsed.TotalSeconds);

            return Ok(new
            {
                archiveId,
                uploadUrl
            });
        }



        [HttpPost("{id:guid}/complete")]
        public async Task<IActionResult> CompleteUpload(
            Guid id,
            [FromBody] InitArchiveUploadRequest request,
            CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("ARCHIVES CONTROLLER: CompleteUpload started, ArchiveName: {ArchiveName}.", request.FileName);

            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var collection = await _db.UploadCollections
                .FirstOrDefaultAsync(c =>
                    c.Id == request.CollectionId &&
                    c.OwnerUserId == userId &&
                    !c.IsDeleted,
                    ct);

            if (collection is null) return NotFound($"Collection {id} not found");

            var extension = Path.GetExtension(request.FileName)?.ToLowerInvariant();
            if (extension != ContentType.Zip) return BadRequest("Only .zip archives are allowed.");

            var storageKey = StorageKeys.Archive(userId, request.CollectionId, id, extension);

            var archive = new ArchiveItem
            {
                Id = id,
                UploadCollectionId = request.CollectionId,
                OriginalFileName = request.FileName,
                StorageKey = storageKey,
                ContentType = ContentType.ApplicationZip,
                SizeBytes = request.FileSize,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.ArchiveItems.Add(archive);
            await _db.SaveChangesAsync(ct);

            sw.Stop();
            _logger.LogInformation("ARCHIVES CONTROLLER: CompleteUpload completed, ArchiveSize: {TotalBytes}, Duration: {Seconds} s",
                request.FileSize,                
                sw.Elapsed.TotalSeconds);

            return Ok(new
            {
                archive.Id,
                archive.OriginalFileName,
                archive.SizeBytes,
                archive.CreatedAtUtc
            });
        }


        [HttpGet("{id:guid}/download-url")]
        public async Task<IActionResult> GetDownloadUrl(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var archive = await LoadOwnedArchive(userId, id, ct);
            if (archive is null) return NotFound();

            var url = await _storage.GeneratePresignedDownloadUrlAsync(
                archive.StorageKey,
                TimeSpan.FromMinutes(Limits.ArchiveItem.GetDownloadUrl.UrlExpiresIn),
                archive.OriginalFileName,
                true);

            return Ok(url);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteArchive(Guid id, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var archive = await LoadOwnedArchive(userId, id, ct);
            if (archive is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(archive.StorageKey))
            {
                await _storage.DeleteAsync(archive.StorageKey, ct);
            }

            _db.ArchiveItems.Remove(archive);
            await _db.SaveChangesAsync(ct);

            sw.Stop();
            _logger.LogInformation("ARCHIVES CONTROLLER: DeleteArchive completed, ArchiveName: {ArchiveName} ArchiveSize: {TotalBytes}, Duration: {Seconds} s",
                archive.OriginalFileName,
                archive.SizeBytes,
                sw.Elapsed.TotalSeconds);

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

        private async Task<ArchiveItem?> LoadOwnedArchive(string userId, Guid archiveId, CancellationToken ct)
        {
            var archive = await _db.ArchiveItems
                .FirstOrDefaultAsync(a =>
                    a.Id == archiveId &&
                    a.UploadCollection.OwnerUserId == userId &&
                    !a.UploadCollection.IsDeleted,
                    ct);

            return archive;
        }
    }
}
