namespace PhotosStorageMap.Domain.ValueObjects
{
    public sealed record ExifData(
        DateTime? TakenAt,
        double? Latitude,
        double? Longitude
    );
}
