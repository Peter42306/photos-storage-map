namespace PhotosStorageMap.Domain.Entities
{
    public class UploadCollection
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string OwnerUserId { get; set; } = string.Empty;

        public string? Title { get; set; }
        public string? Description { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAtUtc { get; set; } // data & time to delete standard photos        

        // stats
        public int PhotosPreviewCount { get; set; }
        public int PhotosDownloadCount { get; set; }
        public int MapPreviewCount { get; set; }
        public int TotalPhotos { get; set; }
        public long TotalBytes { get; set; }

        public ICollection<PhotoItem> Photos { get; set; } = new List<PhotoItem>();
        public ShareLink? ShareLink { get; set; }
    }
}
