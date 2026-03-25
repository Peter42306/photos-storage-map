namespace PhotosStorageMap.Domain.Entities
{
    public class ArchiveItem
    {
        public Guid Id { get; set; }

        public Guid UploadCollectionId { get; set; }
        public UploadCollection UploadCollection { get; set; } = null!;

        public string OriginalFileName { get; set; } = null!;
        public string StorageKey { get; set; } = null!;
        public string ContentType { get; set; } = null!;

        public long SizeBytes { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public string? Description { get; set; }
    }
}
