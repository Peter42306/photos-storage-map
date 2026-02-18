using PhotosStorageMap.Domain.ValueObjects;

namespace PhotosStorageMap.Application.Images
{
    public sealed record ImageProcessResult(
        Stream StandardJpeg,
        Stream ThumbJpeg,
        int Width,
        int Height,
        ExifData Exif
    );
}
