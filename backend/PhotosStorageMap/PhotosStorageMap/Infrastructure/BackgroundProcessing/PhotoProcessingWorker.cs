
using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Infrastructure.Data;
using PhotosStorageMap.Domain.Enums;
using PhotosStorageMap.Application.Common;

namespace PhotosStorageMap.Infrastructure.BackgroundProcessing
{
    public class PhotoProcessingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IPhotoProcessingQueue _queue;
        private readonly ILogger<PhotoProcessingWorker> _logger;

        public PhotoProcessingWorker(
            IServiceScopeFactory scopeFactory,
            IPhotoProcessingQueue queue,
            ILogger<PhotoProcessingWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _queue = queue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PhotoProcessingWorker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                Guid photoId;

                try
                {
                    photoId = await _queue.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch(Exception ex) 
                {
                    _logger.LogError(ex, "Queue dequeue failed.");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                try
                {
                    await ProcessPhotoItemAsync(photoId, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled processing error for photoId={Photoid}", photoId);
                }
            }

            _logger.LogInformation("PhotoProcessingWorker stopped.");
        }


        private async Task ProcessPhotoItemAsync(Guid photoId, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
            var processor = scope.ServiceProvider.GetRequiredService<IImageProcessor>();

            var photo = await db.PhotoItems
                .Include(p => p.UploadCollection)
                .FirstOrDefaultAsync(p => p.Id == photoId, ct);

            if (photo is null)
            {
                _logger.LogWarning("Photo not found: {PhotoId}", photoId);
                return;
            }

            if (string.IsNullOrWhiteSpace(photo.OriginalKey))
            {
                photo.Status = PhotoStatus.Failed;
                photo.Error = "OriginalKey is missing";
                await db.SaveChangesAsync(ct);
                return;
            }

            if (photo.Status == PhotoStatus.Ready)
            {
                return;
            }

            try
            {
                photo.Status = PhotoStatus.Processing;
                photo.Error = null;
                await db.SaveChangesAsync(ct);

                await using var originalStream = await storage.OpenReadAsync(photo.OriginalKey, ct);                

                var result = await processor.ProcessAsync(originalStream, ct);

                long originalBytes = photo.OriginalSizeBytes ?? 0;
                long standardBytes = 0;
                long thumbnailBytes = 0;
                
                if (result.StandardJpeg.CanSeek)
                {
                    result.StandardJpeg.Position = 0;
                    standardBytes = result.StandardJpeg.Length;
                }
                if (result.ThumbJpeg.CanSeek)
                {
                    result.ThumbJpeg.Position = 0;
                    thumbnailBytes = result.ThumbJpeg.Length;
                }                
                var wasCounted = photo.TotalSizeBytes is not null;
                var totalBytes = originalBytes + standardBytes + thumbnailBytes;

                if (!wasCounted)
                {
                    photo.UploadCollection.TotalPhotos += 1;
                    photo.UploadCollection.TotalBytes += totalBytes;
                }

                photo.TotalSizeBytes = totalBytes;
                photo.StandardSizeBytes = standardBytes;
                photo.ThumbSizeBytes = thumbnailBytes;

                var userId = photo.UploadCollection.OwnerUserId;
                var collectionId = photo.UploadCollectionId;

                var standardKey = StorageKeys.Standard(userId, collectionId, photo.Id);
                var thumbKey = StorageKeys.Thumb(userId, collectionId, photo.Id);

                if(result.StandardJpeg.CanSeek) result.StandardJpeg.Position = 0;
                if(result.ThumbJpeg.CanSeek) result.ThumbJpeg.Position = 0;

                await storage.PutAsync(new FileSaveRequest(
                    StorageKey: standardKey,
                    Content: result.StandardJpeg,
                    ContentType: ContentType.ImageJpeg,
                    ContentLength: standardBytes
                ), ct);

                await storage.PutAsync(new FileSaveRequest(
                    StorageKey: thumbKey,
                    Content: result.ThumbJpeg,
                    ContentType: ContentType.ImageJpeg,
                    ContentLength: thumbnailBytes
                ), ct);

                photo.StandardKey = standardKey;
                photo.ThumbKey = thumbKey;
                photo.Width = result.Width;
                photo.Height = result.Height;                

                

                var takenAt = result.Exif.TakenAt;
                if (takenAt.HasValue)
                {
                    var dt = takenAt.Value;
                    if(dt.Kind == DateTimeKind.Unspecified)
                    {
                        //dt = DateTime.SpecifyKind(dt, DateTimeKind.Local);
                        dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    }

                    //photo.TakenAt = dt.ToUniversalTime();
                    photo.TakenAt = dt;
                }
                else
                {                    
                    photo.TakenAt = null;
                }

                photo.Latitude = result.Exif.Latitude;
                photo.Longitude = result.Exif.Longitude;                

                _logger.LogInformation("Latitude: {Latitude}, Longitude: {Longitude}", photo.Latitude, photo.Longitude);
                                
                //if (photo.TotalSizeBytes is null)
                //{
                //    //photo.TotalSizeBytes = totalBytes;
                //    photo.UploadCollection.TotalPhotos += 1;
                //    photo.UploadCollection.TotalBytes += totalBytes;
                //}                

                photo.Status = PhotoStatus.Ready;

                await db.SaveChangesAsync(ct);

                _logger.LogInformation("Processed photoId={Photoid}", photoId);
                _logger.LogInformation("Original size: {OriginalBytes}, standard size: {StandardBytes}, thumbnail size: {ThumbnailBytes}", originalBytes, standardBytes, thumbnailBytes);
                _logger.LogInformation("Total photos {TotalPhotos}, total bytes {TotalBytes}", photo.UploadCollection.TotalPhotos, photo.UploadCollection.TotalBytes);
            }
            catch (Exception ex)
            {
                photo.Status = PhotoStatus.Failed;
                photo.Error = ex.Message;
                photo.TakenAt = null;

                await db.SaveChangesAsync(ct);

                _logger.LogError(ex, "Processing failed photoId={Photoid}", photoId);
            }
        }
    }
}
