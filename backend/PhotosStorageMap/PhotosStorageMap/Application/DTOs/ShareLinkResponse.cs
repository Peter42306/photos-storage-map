namespace PhotosStorageMap.Application.DTOs
{
    public sealed record ShareLinkResponse(
        Guid Id,
        string Token,
        string Url,
        DateTime CreatedAtUtc,
        bool IsRevoked,
        bool AllowSlideshowOriginals,
        bool AllowDownloadResizedZip,
        bool AllowDownloadOriginalFromCard
    );
}
