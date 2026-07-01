using PhotosStorageMap.Domain.Enums;

namespace PhotosStorageMap.Application.DTOs
{
    public sealed record UserStorageLimitsResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;

        public StoragePlan StoragePlan { get; init; }

        public int MaxPhotosPerCollection { get; init; }
        public long MaxAllowedStorageBytes { get; init; }
        public long UsedStorageBytes { get; init; }
        public long RemainingAllowedStorageBytes { get; init; }
    }
}
