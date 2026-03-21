namespace PhotosStorageMap.Application.DTOs
{
    public sealed record ArchiveBuildTempResult(
        string TempFilePath,
        string FileName,
        int FileCount,
        long TotalBytes);    
}
