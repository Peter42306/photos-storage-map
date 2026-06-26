using PhotosStorageMap.Domain.Enums;

namespace PhotosStorageMap.Application.DTOs
{
    public sealed class ZipJobProgressDto
    {
        public Guid JobId { get; set; }
        public ZipJobStatus Status { get; set; } = ZipJobStatus.Running;
        public int ProcessedFiles { get; set; }
        public int TotalFiles { get; set; }
        
        public int Percent => TotalFiles == 0 
            ? 0 
            : ProcessedFiles * 100 / TotalFiles;
        
        public string? Error { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public string ContentType { get; set; } = "application/zip";
    }
}
