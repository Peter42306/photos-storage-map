using PhotosStorageMap.Application.DTOs;

namespace PhotosStorageMap.Application.Interfaces
{
    public interface IZipJobStore
    {
        Guid Create(int totalFiles);
        ZipJobProgressDto? Get(Guid jobId);
        void Update(Guid jobId, int processedFiles);
        void MarkReady(Guid jobId, string filePath, string fileName, string contentType);
        void MarkFailed(Guid jobId, string error);
        void Remove(Guid jobId);
    }
}
