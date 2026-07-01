using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Application.Common;
using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Domain.Enums;
using PhotosStorageMap.Infrastructure.Data;
using PhotosStorageMap.Infrastructure.Identity;

namespace PhotosStorageMap.Infrastructure.Services
{
    public sealed class StorageLimitService : IStorageLimitService
    {
        private readonly ApplicationDbContext _db;

        public StorageLimitService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<UserStorageLimitsResult> GetUserLimitsAsync(
            string userId, 
            CancellationToken ct = default)
        {
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null)
            {
                return new UserStorageLimitsResult
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            var usedStorageBytes = await GetUserStorageUsedBytesAsync(userId, ct);
            var maxAllowedStorageBytes = GetUserStorageLimitBytes(user.StoragePlan);

            return new UserStorageLimitsResult
            {
                Success = true,
                StoragePlan = user.StoragePlan,
                MaxPhotosPerCollection = GetMaxPhotosPerCollection(user.StoragePlan),
                MaxAllowedStorageBytes = maxAllowedStorageBytes,
                UsedStorageBytes = usedStorageBytes,
                RemainingAllowedStorageBytes = Math.Max(0, maxAllowedStorageBytes - usedStorageBytes)
            };
        }

        public async Task<StorageLimitCheckResult> CanAddBytesAsync(
            string userId,
            long newBytes, 
            CancellationToken ct = default)
        {
            var limits = await GetUserLimitsAsync(userId, ct);

            if (!limits.Success)
            {
                return new StorageLimitCheckResult
                {
                    Allowed = false,
                    NewBytes = newBytes,
                    Message = limits.Message
                };
            }

            var allowed = limits.UsedStorageBytes + newBytes <= limits.MaxAllowedStorageBytes;

            return new StorageLimitCheckResult
            {
                Allowed = allowed,
                UsedBytes = limits.UsedStorageBytes,
                NewBytes = newBytes,
                LimitBytes = limits.MaxAllowedStorageBytes,
                Message = allowed ? "" : $"Storage limit exceeded. Used: {limits.UsedStorageBytes} bytes. Limit: {limits.MaxAllowedStorageBytes} bytes."
            };
        }





        private long GetUserStorageLimitBytes(StoragePlan plan)
        {
            switch (plan)
            {
                case StoragePlan.Pro:
                    return Limits.UserStorage.MaxBytesPro;

                default:
                    return Limits.UserStorage.MaxBytesFree;
            }
        }

        private int GetMaxPhotosPerCollection(StoragePlan plan)
        {
            switch (plan)
            {
                case StoragePlan.Pro:
                    return Limits.UploadCollection.MaxPhotosPerCollectionPro;
                default:
                    return Limits.UploadCollection.MaxPhotosPerCollectionFree;
            }
        }

        private async Task<long> GetUserStorageUsedBytesAsync(
            string userId,
            CancellationToken ct)
        {
            return await _db.UploadCollections
                .AsNoTracking()
                .Where(c => c.OwnerUserId == userId && !c.IsDeleted)
                .SumAsync(c => (long?)(c.TotalBytes + c.TotalArchivesBytes), ct) ?? 0;
        }        
    }
}
