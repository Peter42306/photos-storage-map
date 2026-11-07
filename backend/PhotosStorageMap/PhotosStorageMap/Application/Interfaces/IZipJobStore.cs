using PhotosStorageMap.Application.DTOs;

namespace PhotosStorageMap.Application.Interfaces
{
    public interface IZipJobStore
    {
        Guid Create(
            int totalFiles, 
            string ownerUserId);

        Guid CreateForSharedLink(
            int totalFiles,
            Guid sharedLinkId);

        ZipJobProgressDto? Get(Guid jobId);

        IReadOnlyList<ZipJobProgressDto> GetAll();

        void Update(
            Guid jobId, 
            int processedFiles,
            int archivedFiles,
            string? failedFileName = null);

        void MarkReady(
            Guid jobId, 
            string filePath, 
            string fileName,
            string contentType);

        void MarkFailed(
            Guid jobId,
            string error);

        void Remove(Guid jobId);
    }
}
