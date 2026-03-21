using PhotosStorageMap.Domain.Enums;

namespace PhotosStorageMap.Domain.Entities
{
    public class ArchiveExportJob
    {
        public Guid Id { get; set; }

        public Guid CollectionId { get; set; }
        public UploadCollection? Collection { get; set; }
        public string OwnerUserId { get; set; } = string.Empty;

        public ArchiveType Type { get; set; }
        public ArchiveExportJobStatus Status { get; set; }

        public string? StorageKey { get; set; }
        public string? FileName { get; set; }

        public int FileCount { get; set; }
        public long TotalBytes { get; set; }

        public string? Error { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ReadyAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
    }
}
