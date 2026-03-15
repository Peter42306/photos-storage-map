using PhotosStorageMap.Application.DTOs;

namespace PhotosStorageMap.Application.Interfaces
{
    public interface ICollectionStatsService
    {
        Task<CollectionActualStats> GetActualStatsAsync(Guid collectionid, CancellationToken ct = default);
        Task<CollectionActualStats> SyncStoredStatsAsync(Guid collectionid, CancellationToken ct = default);
    }
}
