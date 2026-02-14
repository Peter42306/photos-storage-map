namespace PhotosStorageMap.Application.DTOs.Auth
{
    public sealed record RegisterRequest(string Email, string Password, string? FullName);
}
