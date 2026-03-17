using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Infrastructure.Data;
using PhotosStorageMap.Domain.Enums;

namespace PhotosStorageMap.Infrastructure.Services
{
    public class CollectionStatsService : ICollectionStatsService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CollectionStatsService> _logger;

        public CollectionStatsService(ApplicationDbContext db, ILogger<CollectionStatsService> logger)
        {
            _db = db;
            _logger = logger;
        }

        
        // check actual TotalPhotos & TotalBytes in DB
        public async Task<CollectionActualStats> GetActualStatsAsync(Guid collectionid, CancellationToken ct = default)
        {
            var stats = await _db.PhotoItems
                .AsNoTracking()
                .Where(p => p.UploadCollectionId == collectionid && p.Status == PhotoStatus.Ready)
                .GroupBy(_ => 1)
                .Select(g => new CollectionActualStats(g.Count(), g.Sum(x => x.TotalSizeBytes ?? 0)))
                .SingleOrDefaultAsync(ct);
                //.FirstOrDefaultAsync(ct);

            return stats ?? new CollectionActualStats(0, 0);
        }

        // return to DB checked TotalPhotos & TotalBytes
        public async Task<CollectionActualStats> SyncStoredStatsAsync(Guid collectionid, CancellationToken ct = default)
        {
            var collection = await _db.UploadCollections.FirstOrDefaultAsync(c => c.Id == collectionid);

            if (collection == null)
            {
                throw new InvalidCastException("Collection not found.");
            }

            var actualStats = await GetActualStatsAsync(collectionid, ct);

            var needsUpdate = collection.TotalPhotos != actualStats.TotalPhotos || collection.TotalBytes != actualStats.TotalBytes;

            if (needsUpdate)
            {
                _logger.LogWarning("COLLECTION STATS SERVICE: CollectionId={CollectionId}, StoredPhotos={StoredPhotos}, ActualPhotos={ActualPhotos}, StoredBytes={StoredBytes}, ActualBytes={ActualBytes}",
                    collectionid,
                    collection.TotalPhotos,
                    actualStats.TotalPhotos,
                    collection.TotalBytes,
                    actualStats.TotalBytes);

                collection.TotalPhotos = actualStats.TotalPhotos;
                collection.TotalBytes = actualStats.TotalBytes;
            }

            return actualStats;
        }
    }
}
