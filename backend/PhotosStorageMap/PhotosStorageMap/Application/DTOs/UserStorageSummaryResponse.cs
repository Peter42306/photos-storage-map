namespace PhotosStorageMap.Application.DTOs
{
    public sealed record UserStorageSummaryResponse(
        int TotalCollections,
        int TotalPhotos,
        long TotalPhotosBytes,
        int TotalArchives,        
        long TotalArchivesBytes,
        long TotalStorageBytes);    
}
