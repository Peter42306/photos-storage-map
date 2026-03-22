using PhotosStorageMap.Application.DTOs;

namespace PhotosStorageMap.Application.Interfaces
{
    public interface IArchiveCollectionService
    {
        Task<ArchiveBuildTempResult> BuildStandardZipAsync(Guid collectionId, CancellationToken ct = default);
        Task<ArchiveBuildTempResult> BuildOriginalZipAsync(Guid collectionId, CancellationToken ct = default);
    }
}
