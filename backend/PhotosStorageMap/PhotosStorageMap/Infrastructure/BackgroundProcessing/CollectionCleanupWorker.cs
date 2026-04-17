
using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Application.Common;
using PhotosStorageMap.Infrastructure.Data;
using PhotosStorageMap.Domain.Enums;
using System.Diagnostics;
using PhotosStorageMap.Application.Interfaces;

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
            var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

            var candidates = await db.UploadCollections
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
            var deletedArchivesCount = 0;
            var failedToDeleteArchivesCount = 0;
            var deletedCollectionsCount = 0;

            foreach (var collection in candidates)
            {
                ct.ThrowIfCancellationRequested();

                // Mark photos for background deletion
                var photos = await db.PhotoItems
                    .Where(p => p.UploadCollectionId == collection.Id)
                    .ToListAsync(ct);

                foreach (var photo in photos)
                {
                    if (photo.Status == PhotoStatus.Ready)
                    {
                        photo.Status = PhotoStatus.PendingDelete;
                        markedPhotosCount++;
                    }
                }

                // Delete archives from S3 and DB
                var archives = await db.ArchiveItems
                    .Where(a => a.UploadCollectionId == collection.Id)
                    .ToListAsync(ct);

                foreach (var archive in archives)
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(archive.StorageKey))
                        {
                            await storage.DeleteAsync(archive.StorageKey, ct);

                            _logger.LogInformation(
                                "COLLECTION CLEANUP WORKER: deleted archive from storage archiveId={ArchiveId}, collectionId={CollectionId}, storageKey={StorageKey}",
                                archive.Id,
                                collection.Id,
                                archive.StorageKey);
                        }

                        db.ArchiveItems.Remove(archive);
                        deletedArchivesCount++;
                    }
                    catch (Exception ex)
                    {
                        failedToDeleteArchivesCount++;

                        _logger.LogWarning(
                            ex,
                            "COLLECTION CLEANUP WORKER: failed to delete archive from storage archiveId={ArchiveId}, collectionId={CollectionId}",
                            archive.Id,
                            collection.Id);
                    }
                }

                await db.SaveChangesAsync(ct);

                
                // Remove collection only if no photos and no archives 

                var hasAnyPhotos = await db.PhotoItems.AnyAsync(p => p.UploadCollectionId == collection.Id, ct);
                var hasAnyArchives = await db.ArchiveItems.AnyAsync(a => a.UploadCollectionId == collection.Id, ct);

                if (!hasAnyPhotos && !hasAnyArchives)
                {
                    db.UploadCollections.Remove(collection);
                    deletedCollectionsCount++;

                    _logger.LogInformation(
                        "COLLECTION CLEANUP WORKER: removed collectionId={CollectionId} from DB.",
                        collection.Id);

                    await db.SaveChangesAsync(ct);
                }

            }            

            stopwatch.Stop();            

            _logger.LogInformation(
                "COLLECTION CLEANUP WORKER: candidates={Candidates}, markedPhotos={MarkedPhotos}, deletedArchives={DeletedArchives}, failedArchives={FailedArchives}, deletedCollections={DeletedCollections}, notDeletedCollections={NotDeleted}, duration={Duration:F2}s",
                candidates.Count,
                markedPhotosCount,
                deletedArchivesCount,
                failedToDeleteArchivesCount,
                deletedCollectionsCount,
                candidates.Count - deletedCollectionsCount,
                stopwatch.Elapsed.TotalSeconds);
        }
    }
}
