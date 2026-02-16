namespace PhotosStorageMap.Domain.Entities
{
    public class PhotoItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UploadCollectionId { get; set; }
        public UploadCollection UploadCollection { get; set; } = null!;        

        public string OriginalFileName { get; set; } = string.Empty;

        // storage keys
        public string StandardKey { get; set; } = string.Empty; // standard size photos 1280, userId/uploadId/photoId.jpg
        public string ThumbKey { get; set; } = string.Empty; // thumbnail size photos 320, userId/uploadId/photoId_thumb.jpg

        public int? Width { get; set; }
        public int? Height { get; set; }
        public long? SizeBytes { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // data from EXIF
        public DateTime? TakenAt { get; set; }
        public double? Latitude {  get; set; }
        public double? Longitude { get; set; }

        public DateTime? StandardDeletedAtUtc { get; set; }
    }
}
