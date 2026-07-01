using PhotosStorageMap.Domain.Enums;

namespace PhotosStorageMap.Application.DTOs
{
    public sealed record UpdateUserStoragePlanRequest
    {
        public StoragePlan StoragePlan { get; init; }
    }
}
