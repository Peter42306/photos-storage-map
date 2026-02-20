namespace PhotosStorageMap.Infrastructure.Storage
{
    public sealed class StorageOptions
    {
        public S3Options S3 { get; set; } = new();

        public sealed class S3Options
        {
            public string ServiceUrl { get; set; } = string.Empty;
            public string AccessKey { get; set; } = string.Empty;
            public string SecretKey { get; set; } = string.Empty;
            public string Bucket { get; set; } = string.Empty;
            public bool ForcePathStyle { get; set; } = true;            
        }
    }
}
