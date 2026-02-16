using Microsoft.AspNetCore.Identity;
using PhotosStorageMap.Domain.Enums;

namespace PhotosStorageMap.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public int LoginCount { get; set; } = 0;
        public string? AdminNote { get; set; }
        public StoragePlan StoragePlan { get; set; } = StoragePlan.Free;
    }
}