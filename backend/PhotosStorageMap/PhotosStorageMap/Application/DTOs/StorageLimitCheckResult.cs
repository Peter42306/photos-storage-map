namespace PhotosStorageMap.Application.DTOs
{
    public sealed record StorageLimitCheckResult
    {
        public bool Allowed { get; init; }
        public string Message { get; init; } = string.Empty;
                
        public long UsedBytes { get; init; }
        public long NewBytes { get; init; }
        public long LimitBytes { get; init; }        
    }
}
