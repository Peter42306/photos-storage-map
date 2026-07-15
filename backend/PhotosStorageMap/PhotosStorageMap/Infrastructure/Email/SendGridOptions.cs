namespace PhotosStorageMap.Infrastructure.Email
{
    public sealed class SendGridOptions
    {
        public string ApiKey { get; init; } = string.Empty;
        public string FromEmail { get; init; } = string.Empty;
        public string FromName { get; init; } = "PhotoMap";
    }
}
