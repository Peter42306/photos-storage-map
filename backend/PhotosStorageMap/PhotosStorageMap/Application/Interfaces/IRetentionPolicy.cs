using PhotosStorageMap.Infrastructure.Identity;

namespace PhotosStorageMap.Application.Interfaces
{
    public interface IRetentionPolicy
    {
        DateTime? GetStandardExpiresAtUtc(ApplicationUser user, DateTime createdAtUtc);
    }
}
