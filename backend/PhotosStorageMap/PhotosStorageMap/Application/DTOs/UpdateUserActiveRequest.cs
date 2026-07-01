namespace PhotosStorageMap.Application.DTOs
{
    public sealed record UpdateUserActiveRequest
    {
        public bool IsActive { get; init; }
    }
}
