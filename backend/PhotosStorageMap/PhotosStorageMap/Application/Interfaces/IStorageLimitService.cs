using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Domain.Enums;

namespace PhotosStorageMap.Application.Interfaces
{
    public interface IStorageLimitService
    {
        Task<UserStorageLimitsResult> GetUserLimitsAsync(
            string userId,
            CancellationToken ct = default);

        Task<StorageLimitCheckResult> CanAddBytesAsync(
            string userId,
            long newBytes,
            CancellationToken ct = default);
    }
}
