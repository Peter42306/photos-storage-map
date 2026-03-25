namespace PhotosStorageMap.Application.DTOs
{
    public sealed record InitArchiveUploadRequest(
        Guid CollectionId,
        string FileName,
        long FileSize);
    
}
