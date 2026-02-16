using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Domain.Entities;
using PhotosStorageMap.Infrastructure.Identity;
using PhotosStorageMap.Domain.Enums;

namespace PhotosStorageMap.Infrastructure.Policies
{
    public class DefaultRetentionPolicy : IRetentionPolicy
    {
        public DateTime? GetStandardExpiresAtUtc(ApplicationUser user, DateTime createdAtUtc)
        {
            if (user.StoragePlan == StoragePlan.Pro)
            {
                return null;
            }

            return createdAtUtc.AddDays(10);
        }

        
        public bool CanDownload(ApplicationUser user, UploadCollection uploadCollection)
        {
            if (user.StoragePlan == StoragePlan.Pro)
            {
                return true;
            }

            if (uploadCollection.ExpiresAtUtc is null)
            {
                return true;
            }

            bool canDownload = DateTime.UtcNow <= uploadCollection.ExpiresAtUtc.Value;

            return canDownload;
        }
    }
}
