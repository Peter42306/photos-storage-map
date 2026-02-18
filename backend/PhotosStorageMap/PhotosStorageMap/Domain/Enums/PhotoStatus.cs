namespace PhotosStorageMap.Domain.Enums
{
    // Status of original file uploaded from user to disk or S3 before processing and removal
    public enum PhotoStatus
    {
        Uploaded = 0,
        Processing = 1,
        Ready = 2,
        Failed = 3
    }
}
