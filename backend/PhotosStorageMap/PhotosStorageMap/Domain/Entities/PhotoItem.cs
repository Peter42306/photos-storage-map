using PhotosStorageMap.Domain.Enums;

namespace PhotosStorageMap.Domain.Entities
{
    public class PhotoItem
    {
        public Guid Id { get; set; }

        public Guid UploadCollectionId { get; set; }
        public UploadCollection UploadCollection { get; set; } = null!;        

        public string OriginalFileName { get; set; } = string.Empty;
        public string? Description { get; set; }

        // processed images
        public string? StandardKey { get; set; } // resized image, standard size photos 1280, userId/uploadId/photoId.jpg
        public string? ThumbKey { get; set; } // resized image, thumbnail size photos 320, userId/uploadId/photoId_thumb.jpg

        public int? Width { get; set; }
        public int? Height { get; set; }
        
        public long? OriginalSizeBytes { get; set; }
        public long? StandardSizeBytes { get; set; }
        public long? ThumbSizeBytes { get; set; }
        public long? TotalSizeBytes { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // EXIF
        public DateTime? TakenAt { get; set; }
        public double? Latitude {  get; set; }
        public double? Longitude { get; set; }

        // status of original photos temporarily loaded to S3 before delete
        public string? OriginalKey { get; set; }

        // processing lifecycle
        public PhotoStatus Status { get; set; } = PhotoStatus.Uploading;
        public string? Error { get; set; }

        public DateTime? StandardDeletedAtUtc { get; set; }
    }
}
