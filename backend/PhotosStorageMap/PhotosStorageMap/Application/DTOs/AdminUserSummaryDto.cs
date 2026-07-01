using PhotosStorageMap.Domain.Enums;

namespace PhotosStorageMap.Application.DTOs
{
    public sealed record AdminUserSummaryDto
    {
        public string UserId { get; init; } = string.Empty;
        public string? Email { get; init; }
        public string? FullName { get; init; }

        public bool IsActive { get; init; }
        public bool EmailConfirmed { get; init; }

        public StoragePlan StoragePlan { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public int LoginCount { get; init; }

        public int CollectionsCount { get; init; }
        public int PhotosCount { get; init; }
        public int ArchivesCount { get; init; }

        public long PhotosBytes {  get; init; }
        public long ArchivesBytes { get; init; }
        public long TotalStorageBytes { get; init; }        

        public string? AdminNote { get; init; }
    }
}
