using PhotosStorageMap.Application.Images;

namespace PhotosStorageMap.Application.Interfaces
{
    public interface IImageProcessor
    {
        Task<ImageProcessResult> ProcessAsync(Stream original, CancellationToken ct = default);
    }
}
