namespace PhotosStorageMap.Application.Common
{
    public static class StorageKeys
    {
        public static string Original(string userId, Guid collectionId, Guid photoId)
        {
            return $"{userId}/{collectionId}/photos/{photoId}_original.jpg";
        }

        public static string Standard(string userId, Guid collectionId, Guid photoId)
        {
            return $"{userId}/{collectionId}/photos/{photoId}_standard.jpg";
        }

        public static string Thumb(string userId, Guid collectionId, Guid photoId)
        {
            return $"{userId}/{collectionId}/photos/{photoId}_thumbnail.jpg";
        }

        public static string Archive(string userId, Guid collectionId, Guid archiveId, string extension)
        {
            return $"{userId}/{collectionId}/archives/{archiveId}{extension}";
        }
    }
}
