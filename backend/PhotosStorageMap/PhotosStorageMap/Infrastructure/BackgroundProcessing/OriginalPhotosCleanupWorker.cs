
using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Application.Common;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Domain.Entities;
using PhotosStorageMap.Infrastructure.Data;
using System.Diagnostics;
using PhotosStorageMap.Domain.Enums;

namespace PhotosStorageMap.Infrastructure.BackgroundProcessing
{
    public class OriginalPhotosCleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<OriginalPhotosCleanupWorker> _logger;

        private static readonly TimeSpan LoopDelay = TimeSpan.FromMinutes(Limits.OriginalPhotosCleanupWorker.LoopDelay);

        public OriginalPhotosCleanupWorker(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<OriginalPhotosCleanupWorker> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;            
        }



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ORIGINAL PHOTOS CLEANUP WORKER: started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {                    
                    await CleanupAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ORIGINAL PHOTOS CLEANUP WORKER: loop failed.");
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

            _logger.LogInformation("ORIGINAL PHOTOS CLEANUP WORKER: stopped.");
        }

        private async Task CleanupAsync(CancellationToken ct)
        {
            var stopwatch = Stopwatch.StartNew();

            using var scope = _serviceScopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

            var photos = await db.PhotoItems
                .Include(p => p.UploadCollection)
                .Where(p =>
                    p.Status == PhotoStatus.Ready &&
                    p.OriginalDeleteRequested &&
                    p.OriginalKey != null &&
                    p.OriginalKey != "" &&
                    !p.UploadCollection.IsDeleted)
                .OrderBy(p => p.OriginalDeleteRequestedAtUtc)
                .Take(Limits.OriginalPhotosCleanupWorker.BatchSize)
                .ToListAsync(ct);

            if (photos.Count == 0)
            {
                _logger.LogInformation("ORIGINAL PHOTOS CLEANUP WORKER: no originals to delete found.");
                return;
            }

            var deletedOriginalsCount = 0;
            var failedOriginalsCount = 0;
            long freedBytes = 0;

            foreach ( var photo in photos )
            {
                ct.ThrowIfCancellationRequested();

                var originalSizeBytes = photo.OriginalSizeBytes ?? 0;

                var deleted = await DeleteOriginalPhotoAsync(storage, photo, ct);

                if (deleted)
                {
                    deletedOriginalsCount++;
                    freedBytes += originalSizeBytes;
                }
                else
                {
                    failedOriginalsCount++;
                }                
            }

            await db.SaveChangesAsync(ct);

            stopwatch.Stop();

            _logger.LogInformation(
                "ORIGINAL PHOTOS CLEANUP WORKER: processed={Processed}, deleted={Deleted}, failed={Failed}, freedBytes={FreedBytes}, duration={Duration:F2}s",
                photos.Count,
                deletedOriginalsCount,
                failedOriginalsCount,
                freedBytes,
                stopwatch.Elapsed.TotalSeconds);
        }

        private async Task<bool> DeleteOriginalPhotoAsync(
            IFileStorage storage,
            PhotoItem photo,
            CancellationToken ct)
        {
            var originalKey = photo.OriginalKey;
            var originalSizeBytes = photo.OriginalSizeBytes ?? 0L;

            if (string.IsNullOrWhiteSpace(originalKey))
            {
                return false;
            }

            try
            {
                await storage.DeleteAsync(originalKey, ct);

                photo.OriginalKey = null;
                photo.OriginalSizeBytes = 0;
                photo.TotalSizeBytes = (photo.StandardSizeBytes ?? 0L) + (photo.ThumbSizeBytes ?? 0L);

                photo.OriginalDeleteRequested = false;
                photo.OriginalDeletedAtUtc = DateTime.UtcNow;
                photo.OriginalDeleteError = null;                

                photo.UploadCollection.TotalBytes = Math.Max(0L, photo.UploadCollection.TotalBytes - originalSizeBytes);

                _logger.LogInformation(
                    "ORIGINAL CLEANUP WORKER: deleted original. PhotoId={PhotoId}, CollectionId={CollectionId}, Key={StorageKey}, FreedBytes={FreedBytes}",
                    photo.Id,
                    photo.UploadCollectionId,
                    originalKey,
                    originalSizeBytes);

                return true;
            }
            catch (Exception ex)
            {
                photo.OriginalDeleteError = ex.Message;

                _logger.LogWarning(
                    ex,
                    "ORIGINAL CLEANUP WORKER: failed to delete original. PhotoId={PhotoId}, CollectionId={CollectionId}, Key={StorageKey}",
                    photo.Id,
                    photo.UploadCollectionId,
                    originalKey);

                return false;                
            }
        }
    }
}
