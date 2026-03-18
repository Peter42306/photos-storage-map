using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Application.Common;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Infrastructure.Data;
using PhotosStorageMap.Domain.Enums;
using PhotosStorageMap.Domain.Entities;
using System.Diagnostics;

namespace PhotosStorageMap.Infrastructure.BackgroundProcessing
{
    public class PhotoCleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<PhotoCleanupWorker> _logger;

        private static readonly TimeSpan LoopDelay = TimeSpan.FromMinutes(Limits.PhotoCleanupWorker.LoopDelay);
        private static readonly TimeSpan StatusUploadingOlderThan = TimeSpan.FromHours(Limits.PhotoCleanupWorker.StatusUploadingOlderThan);
        private static readonly TimeSpan StatusProcessingOlderThan = TimeSpan.FromHours(Limits.PhotoCleanupWorker.StatusProcessingOlderThan);
        private static readonly TimeSpan StatusFailedOlderThan = TimeSpan.FromHours(Limits.PhotoCleanupWorker.StatusFailedOlderThan);
        

        public PhotoCleanupWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<PhotoCleanupWorker> logger)
        {
            _serviceScopeFactory = scopeFactory;
            _logger = logger;
        }

        
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {            
            _logger.LogInformation("PHOTO CLEANUP WORKER: PhotoCleanupWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupAsync(stoppingToken);
                }
                catch(OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PHOTO CLEANUP WORKER: loop failed.");
                }

                try
                {
                    await Task.Delay(LoopDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
         
            _logger.LogInformation("PHOTO CLEANUP WORKER: PhotoCleanupWorker stopped.");
        }

        private async Task CleanupAsync(CancellationToken ct)
        {
            var stopwatch = Stopwatch.StartNew();

            using var scope = _serviceScopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

            var now = DateTime.UtcNow;

            var statusUploadingThreshold = now - StatusUploadingOlderThan;
            var statusFailedThreshold = now - StatusFailedOlderThan;
            var statusProcessingThreshold = now - StatusProcessingOlderThan;

            var query = db.PhotoItems
                .Where(p =>
                    (p.Status == PhotoStatus.Uploading && p.CreatedAtUtc < statusUploadingThreshold) ||
                    (p.Status == PhotoStatus.Processing && p.CreatedAtUtc < statusProcessingThreshold) ||
                    (p.Status == PhotoStatus.Failed && p.CreatedAtUtc < statusFailedThreshold) ||
                    (p.Status == PhotoStatus.PendingDelete));
                        
            var uploadingCandidates = await query.Where(p => p.Status == PhotoStatus.Uploading).CountAsync(ct);
            var processingCandidates = await query.Where(p => p.Status == PhotoStatus.Processing).CountAsync(ct);
            var failedCandidates = await query.Where(p => p.Status == PhotoStatus.Failed).CountAsync(ct);            
            var pendingDeleteCandidates = await query.Where(p => p.Status == PhotoStatus.PendingDelete).CountAsync(ct);
            var totalCandidates = uploadingCandidates + processingCandidates + failedCandidates + pendingDeleteCandidates;

            var candidates = await query
                    .OrderBy(p => p.CreatedAtUtc)
                    .Take(Limits.PhotoCleanupWorker.BatchSize)
                    .ToListAsync(ct);

            var batchSize = candidates.Count;

            if (batchSize == 0)
            {
                _logger.LogInformation("PHOTO CLEANUP WORKER: no candidates to delete found");
                return;
            }            

            _logger.LogInformation("PHOTO CLEANUP WORKER: found TotalCandidates={TotalCandidates}, Uploading={Uploading}, Processing={Processing} PendingDelete={PendingDelete}, Failed={Failed}, BatchSize {BatchSize} candidates for cleanup.",
                totalCandidates,
                uploadingCandidates,
                processingCandidates,
                pendingDeleteCandidates,
                failedCandidates,
                batchSize);

            var deletedCount = 0;
            var failedCount = 0;

            foreach (var photo in candidates)
            {
                ct.ThrowIfCancellationRequested();

                var deleted = await CleanupPhotoAsync(db, storage, photo, ct);

                if (deleted)
                {
                    deletedCount++;

                    _logger.LogInformation("PHOTO CLEANUP WORKER: deleted Photo={PhotoName} PhotoId={PhotoId}",
                    photo.OriginalFileName,
                    photo.Id);
                }
                else
                {
                    failedCount++;
                }                
                
            }

            await db.SaveChangesAsync(ct);

            var remainedCandidates = Math.Max(totalCandidates - batchSize, 0);

            stopwatch.Stop();

            _logger.LogInformation("PHOTO CLEANUP WORKER: processed: {Processed}, deleted: {Count}, failed: {Failed}, remained to delete: {remained}, duration: {Duration:F2} seconds.", 
                batchSize,
                deletedCount,
                failedCount,
                remainedCandidates,
                stopwatch.Elapsed.TotalSeconds);

            
        }

        private async Task<bool> CleanupPhotoAsync(
            ApplicationDbContext db,
            IFileStorage storage,
            PhotoItem photo,
            CancellationToken ct)
        {
            var photoId = photo.Id;
            var allDeleted = true;

            var keys = new[]
            {
                (Key: photo.OriginalKey, Type: ContentType.Original),
                (Key: photo.StandardKey, Type: ContentType.Standard),
                (Key: photo.ThumbKey, Type: ContentType.Thumbnail),
            };

            foreach ( var item in keys )
            {
                if (string.IsNullOrWhiteSpace(item.Key))
                {
                    continue;
                }

                try
                {
                    await storage.DeleteAsync(item.Key, ct);

                    _logger.LogInformation("PHOTO CLEANUP WORKER: deleted from S3 {FileType} file for PhotoId={PhotoId}, Key={StorageKey}",
                        item.Type,
                        photoId,
                        item.Key);
                }
                catch (Exception ex)
                {
                    allDeleted = false;

                    _logger.LogWarning(
                        ex,
                        "PHOTO CLEANUP WORKER: failed deleting from S3 {FileType} file for PhotoId={PhotoId}, Key={StorageKey}",
                        item.Type,
                        photoId,
                        item.Key);
                }
            }

            if (!allDeleted)
            {
                _logger.LogWarning("PHOTO CLEANUP WORKER: photoId={PhotoId} was not removed from DB because some storage files failed to delete.", photoId);
                return false;
            }

            db.PhotoItems.Remove(photo);

            _logger.LogInformation(
                "PHOTO CLEANUP WORKER: removed from DB photoId={photoId}, status={Status}",
                photoId,
                photo.Status);

            return true;
        }
    }
}
