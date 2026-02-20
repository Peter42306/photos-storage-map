namespace PhotosStorageMap.Application.Interfaces
{
    public interface IPhotoProcessingQueue
    {
        ValueTask EnqueueAsync(Guid photoId, CancellationToken ct = default);
        ValueTask<Guid> DequeueAsync(CancellationToken ct = default);
    }
}
