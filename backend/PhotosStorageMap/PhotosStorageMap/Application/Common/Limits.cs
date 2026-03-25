namespace PhotosStorageMap.Application.Common
{
    public static class Limits
    {
        public static class UploadCollection
        {
            public const int Title = 200;
            public const int Description = 4000;
            public const int MaxPhotosPerCollectionPro = 1500; // 2000 TODO:
            public const int MaxPhotosPerCollectionFree = 500; // 1000
            public const long MaxBytesPerCollectionPro = 10L * 1024 * 1024 * 1024; // 10 GB
            public const long MaxBytesPerCollectionFree = 5L * 1024 * 1024 * 1024; // 5 GB
        }

        public static class PhotoItem
        {
            public const int OriginalFileName = 255;
            public const int StorageKey = 1024;
            public const int Description = 2000;
            public const int Error = 2000;
        }

        public static class ShareLink
        {
            public const int Token = 128;
        }        

        public static class PhotoCleanupWorker
        {
            public const int BatchSize = 20; // 20 photos
            public const int LoopDelay = 5; // 5 minutes
            public const int StatusUploadingOlderThan = 1; // 3 hours
            public const int StatusProcessingOlderThan = 1; // 1 hours
            public const int StatusFailedOlderThan = 1; // 24 hours
        }

        public static class CollectionCleanupWorker
        {
            public const int BatchSize = 20; // 20 photos
            public const int LoopDelay = 7; // 5 minutes
        }

        public static class ArchiveItem
        {
            public const int OriginalFileName = 255;
            public const int StorageKey = 1024;
            public const int Description = 1000;
            public const int ContentType = 100;            
            public const long MaxSizeBytes = 20L * 1024 * 1024 * 1024; // 20 GB
            public const int Error = 2000;
        }
    }
}
