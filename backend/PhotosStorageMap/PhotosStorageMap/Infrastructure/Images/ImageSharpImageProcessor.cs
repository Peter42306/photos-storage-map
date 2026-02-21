using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using PhotosStorageMap.Application.Common;
using PhotosStorageMap.Application.Images;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Domain.ValueObjects;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace PhotosStorageMap.Infrastructure.Images
{
    public sealed class ImageSharpImageProcessor : IImageProcessor
    {
        public async Task<ImageProcessResult> ProcessAsync(Stream original, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (original is null)
            {
                throw new ArgumentNullException(nameof(original));
            }

            await using var buffered = new MemoryStream();
            await original.CopyToAsync(buffered, ct);
            buffered.Position = 0;

            // 1) EXIF/GPS (will read from buffered)
            var exif = ReadExifWithMetadataExtractor(buffered);

            // Need to rewind for ImageSharp
            buffered.Position = 0;
            
            //if (!original.CanSeek)
            //{
            //    throw new InvalidOperationException("Original stream must be seekable.");
            //}

            //original.Position = 0;

            // 2) Image processing
            using var image = await Image.LoadAsync(buffered, ct);
            image.Mutate(x => x.AutoOrient());

            var width = image.Width;
            var height = image.Height;

            var standardStream = await ResizeAndEncodeJpegAsync(
                image,
                ImageSettings.MaxSideSize.Standard,
                ImageSettings.Quality.Standard,
                ct
            );

            var thumbStream = await ResizeAndEncodeJpegAsync(
                image,
                ImageSettings.MaxSideSize.Thumbnail,
                ImageSettings.Quality.Thumbnail,
                ct
            );

            return new ImageProcessResult(
                StandardJpeg: standardStream,
                ThumbJpeg: thumbStream,
                Width: width,
                Height: height,
                Exif: exif
            );

        }


        //-----------------------------------------------------------
        // Image processing via SixLabors.ImageSharp
        //-----------------------------------------------------------

        private static async Task<MemoryStream> ResizeAndEncodeJpegAsync(
            SixLabors.ImageSharp.Image source,
            int maxSide,
            int quality,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            using var resized = source.Clone(ctx => ResizeToFit(ctx, maxSide));

            var ms = new MemoryStream();
            await resized.SaveAsJpegAsync(ms, new JpegEncoder { Quality = quality }, ct);
            ms.Position = 0;

            return ms;
        }
        
        private static void ResizeToFit(IImageProcessingContext ctx, int maxSide)
        {
            ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxSide, maxSide)
            });
        }

        //-----------------------------------------------------------
        // EXIF/GPS via MetadataExtractor
        //-----------------------------------------------------------

        private static ExifData ReadExifWithMetadataExtractor(Stream stream)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            IReadOnlyList<MetadataExtractor.Directory> directories;
            try
            {
                directories = ImageMetadataReader.ReadMetadata(stream);
            }
            catch
            {
                return new ExifData(null, null, null);                
            }

            DateTime? takenAt = null;
            double? lat = null;
            double? lon = null;

            // DateTimeOriginal / DateTimeDigitized
            var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (subIfd is not null)
            {
                if (subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dto))
                {
                    takenAt = dto;
                }
                else if (subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeDigitized, out var dtd))
                {
                    takenAt = dtd;
                }
            }

            //GPS
            var gpsDir = directories.OfType<GpsDirectory>().FirstOrDefault();            
            if (gpsDir is not null && gpsDir.TryGetGeoLocation(out var location))
            {
                lat = location.Latitude;
                lon = location.Longitude;                
            }

            return new ExifData(takenAt, lat, lon);
        }
    }
}
