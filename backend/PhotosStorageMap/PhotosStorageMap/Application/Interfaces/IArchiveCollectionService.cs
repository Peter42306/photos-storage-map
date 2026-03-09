using PhotosStorageMap.Application.DTOs;

namespace PhotosStorageMap.Application.Interfaces
{
    public interface IArchiveCollectionService
    {
        Task<ArchiveBuildResult> BuildStandardZipAsync(Guid collectionId, CancellationToken ct = default);
        Task<ArchiveBuildResult> BuildOriginalZipAsync(Guid collectionId, CancellationToken ct = default);
    }
}
