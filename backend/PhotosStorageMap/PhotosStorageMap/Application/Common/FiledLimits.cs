namespace PhotosStorageMap.Application.Common
{
    public static class FieldLimits
    {
        public static class UploadCollection
        {
            public const int Title = 200;
            public const int Description = 4000;
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
    }
}
