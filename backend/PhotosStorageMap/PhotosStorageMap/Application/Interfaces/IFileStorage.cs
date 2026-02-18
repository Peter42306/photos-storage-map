namespace PhotosStorageMap.Application.Interfaces
{
    public interface IFileStorage
    {
        Task<string> PutAsync(FileSaveRequest request, CancellationToken ct = default);
        Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default);
        Task<bool> DeleteAsync(string storageKey, CancellationToken ct = default);        
    }
}
