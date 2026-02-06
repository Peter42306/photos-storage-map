namespace PhotosStorageMap.Application.DTOs.Auth
{
    public record AuthResponse(string AccessToken, DateTime ExpiresAtUtc);
}
