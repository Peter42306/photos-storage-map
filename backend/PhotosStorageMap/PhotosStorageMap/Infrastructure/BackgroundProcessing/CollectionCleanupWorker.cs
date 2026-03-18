
using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Application.Common;
using PhotosStorageMap.Infrastructure.Data;
using PhotosStorageMap.Domain.Enums;
using System.Diagnostics;

namespace PhotosStorageMap.Infrastructure.BackgroundProcessing
{
    public class CollectionCleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<CollectionCleanupWorker> _logger;

        private static readonly TimeSpan LoopDelay = TimeSpan.FromMinutes(Limits.CollectionCleanupWorker.LoopDelay);

        public CollectionCleanupWorker(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<CollectionCleanupWorker> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("COLLECTION CLEANUP WORKER: PhotoCleanupWorker started.");

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
                    _logger.LogError(ex, "COLLECTION CLEANUP WORKER: loop failed.");
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

            _logger.LogInformation("COLLECTION CLEANUP WORKER: PhotoCleanupWorker stopped.");
        }

        private async Task CleanupAsync(CancellationToken ct)
        {
            var stopwatch = Stopwatch.StartNew();

            using var scope = _serviceScopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var candidates = await db.UploadCollections
                .Include(c => c.Photos)
                .Where(c => c.IsDeleted)
                .OrderBy(c => c.CreatedAtUtc)
                .Take(Limits.CollectionCleanupWorker.BatchSize)
                .ToListAsync(ct);

            if (candidates.Count == 0)
            {
                _logger.LogInformation("COLLECTION CLEANUP WORKER: no candidates to delete found");
                return;
            }

            var markedPhotosCount = 0;
            var deletedCollectionsCount = 0;

            foreach (var collection in candidates)
            {
                ct.ThrowIfCancellationRequested();

                foreach (var photo in collection.Photos)
                {
                    if (photo.Status == PhotoStatus.Ready)
                    {
                        photo.Status = PhotoStatus.PendingDelete;
                        markedPhotosCount++;
                    }
                }

                //var hasAnyPhotosLeft = collection.Photos.Any();
                var hasAnyPhotosLeft = await db.PhotoItems.AnyAsync(p => p.UploadCollectionId == collection.Id, ct);

                if (!hasAnyPhotosLeft)
                {
                    db.UploadCollections.Remove(collection);
                    deletedCollectionsCount++;

                    _logger.LogInformation("COLLECTION CLEANUP WORKER: removed collectionId={CollectionId} from DB.",
                        collection.Id);
                }
            }

            await db.SaveChangesAsync();

            stopwatch.Stop();

            _logger.LogInformation(
                "COLLECTION CLEANUP WORKER: candidates to delete {Processed}, marked photos as PendingDelete {MarkedPhotos}, deleted collections {DeletedCollections}, duration: {Duration:F2} seconds.",
                candidates.Count,
                markedPhotosCount,
                deletedCollectionsCount, 
                stopwatch.Elapsed.TotalSeconds);
        }
    }
}
