namespace PhotosStorageMap.Application.DTOs
{
    public sealed record CreateOrUpdateShareLinkRequest(
        Guid CollectionId,
        bool AllowSlideshowOriginals,
        bool AllowDownloadResizedZip,
        bool AllowDownloadOriginalFromCard
    );
}
