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

        // ------------------------------------------------------------
        // Photo analytics and storage summary
        // ------------------------------------------------------------

        // Number of times photo preview/gallery was opened
        public int PhotosPreviewCount { get; set; }

        // Number of times original photos were downloaded
        public int PhotosDownloadCount { get; set; }

        // Number of times map view was opened
        public int MapPreviewCount { get; set; }

        // Total number of photos currently stored in this collection
        public int TotalPhotos { get; set; }

        // Total storage size used by photos in this collection (bytes)
        // NOTE: this is photo storage summary, not traffic
        public long TotalBytes { get; set; }

        // ------------------------------------------------------------
        // Archive analytics and storage summary
        // ------------------------------------------------------------

        // Number of times archives were downloaded
        public int ArchiveDownloadCount { get; set; }

        // Number of archive upload operations performed        
        public int ArchiveUploadCount { get; set; }

        // Total archive download traffic in bytes
        public long ArchiveDownloadBytes { get; set; }

        // Total uploaded archive bytes
        public long ArchiveUploadBytes { get; set; }

        // Total number of archives currently stored in this collection
        public int TotalArchives { get; set; }

        // Total storage size used by archives in this collection
        public long TotalArchivesBytes { get; set; }

                
        public bool IsDeleted { get; set; }

        public ICollection<PhotoItem> Photos { get; set; } = new List<PhotoItem>();
        public ICollection<ArchiveItem> Archives { get; set; } = new List<ArchiveItem>();

        public ShareLink? ShareLink { get; set; }


        public long TotalStorageBytes => TotalBytes + TotalArchivesBytes;
    }    
}
