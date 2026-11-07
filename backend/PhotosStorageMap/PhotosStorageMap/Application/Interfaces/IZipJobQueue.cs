using PhotosStorageMap.Application.DTOs;

namespace PhotosStorageMap.Application.Interfaces
{
    public interface IZipJobQueue
    {
        bool TryEnqueue(ZipJobRequest request);

        ValueTask<ZipJobRequest> DequeueAsync(CancellationToken ct);
    }
}
