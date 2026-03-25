namespace PhotosStorageMap.Domain.Entities
{
    public class UploadCollection
    {
        public Guid Id { get; set; }
        public string OwnerUserId { get; set; } = string.Empty;

        public string? Title { get; set; }
        public string? Description { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAtUtc { get; set; } // data & time to delete standard photos        

        // stats for Photoitem
        public int PhotosPreviewCount { get; set; }
        public int PhotosDownloadCount { get; set; }
        public int MapPreviewCount { get; set; }
        public int TotalPhotos { get; set; }
        public long TotalBytes { get; set; }

        // stats for Archives
        public int ArchiveDownloadCount { get; set; }
        public int ArchiveUploadCount { get; set; }
        public long ArchiveDownloadBytes { get; set; }
        public long ArchiveUploadBytes { get; set; }
                
        public bool IsDeleted { get; set; }

        public ICollection<PhotoItem> Photos { get; set; } = new List<PhotoItem>();
        public ICollection<ArchiveItem> Archives { get; set; } = new List<ArchiveItem>();

        public ShareLink? ShareLink { get; set; }
    }
}
