namespace PhotosStorageMap.Application.Interfaces
{
    public record FileSaveRequest(
        string StorageKey,  // "userId/collectionId/photoId.jpg"
        Stream Content,        
        string? ContentType = null,
        long? ContentLength = null
    );
}
