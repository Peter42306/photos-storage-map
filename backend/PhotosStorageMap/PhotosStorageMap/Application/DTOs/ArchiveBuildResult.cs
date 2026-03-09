namespace PhotosStorageMap.Application.DTOs
{
    public sealed record ArchiveBuildResult(
        Stream Stream,
        string FileName,
        string ContentType,
        int FilesCount,
        long TotalBytes
    );
}
