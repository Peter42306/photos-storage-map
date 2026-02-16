namespace PhotosStorageMap.Domain.Entities
{
    public class ShareLink
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UploadCollectionId { get; set; }
        public UploadCollection UploadCollection { get; set; } = null!;

        public string Token { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAtUtc { get; set; }

        public bool IsRevoked { get; set; } = false;
        public bool AllowDownload { get; set; } = true;
    }
}
