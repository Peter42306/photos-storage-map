namespace PhotosStorageMap.Application.Common
{
    public static class StorageKeys
    {
        public static string Original(string userId, Guid collectionId, Guid photoId)
        {
            return $"{userId}/{collectionId}/{photoId}_original.jpg";
        }

        public static string Standard(string userId, Guid collectionId, Guid photoId)
        {
            return $"{userId}/{collectionId}/{photoId}.jpg";
        }

        public static string Thumb(string userId, Guid collectionId, Guid photoId)
        {
            return $"{userId}/{collectionId}/{photoId}_thumb.jpg";
        }
    }
}
