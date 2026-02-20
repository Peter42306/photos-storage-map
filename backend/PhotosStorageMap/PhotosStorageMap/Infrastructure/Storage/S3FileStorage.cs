using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using PhotosStorageMap.Application.Interfaces;

namespace PhotosStorageMap.Infrastructure.Storage
{
    public sealed class S3FileStorage : IFileStorage
    {
        private readonly IAmazonS3 _s3;
        private readonly StorageOptions.S3Options _options;

        public S3FileStorage(IAmazonS3 s3, IOptions<StorageOptions> options)
        {
            _s3 = s3;
            _options = options.Value.S3;
        }

        public async Task<string> PutAsync(FileSaveRequest request, CancellationToken ct = default)
        {
            if (request is null) 
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.StorageKey)) 
                throw new ArgumentException("Storage key is required", nameof(request));

            if (request.Content is null) 
                throw new ArgumentException("Content is required", nameof(request));

            if(request.Content.CanSeek) request.Content.Position = 0;
            ct.ThrowIfCancellationRequested();

            var put = new PutObjectRequest
            {
                BucketName = _options.Bucket,
                Key = request.StorageKey,
                InputStream = request.Content,
                ContentType = request.ContentType ?? "application/octet-stream",
            };

            await _s3.PutObjectAsync(put, ct);
            return request.StorageKey;
        }

        public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default)
        {
            if(string.IsNullOrWhiteSpace(storageKey)) 
                throw new ArgumentException("Storage key is required.",nameof(storageKey));

            ct.ThrowIfCancellationRequested();

            var resp = await _s3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _options.Bucket,
                Key = storageKey
            }, ct);

            return resp.ResponseStream;
        }
        
        public async Task<bool> DeleteAsync(string storageKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(storageKey)) return false;
            
            ct.ThrowIfCancellationRequested();
            
            await _s3.DeleteObjectAsync(_options.Bucket, storageKey, ct);
            
            return true;
        }

        public Task<string> GeneratePresignedUploadUrlAsync(string storageKey, TimeSpan expiresIn)
        {
            if (string.IsNullOrWhiteSpace(storageKey)) 
                throw new ArgumentException("Storage key is required.",nameof(storageKey));

            var req = new GetPreSignedUrlRequest
            {
                BucketName = _options.Bucket,
                Key = storageKey,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.Add(expiresIn)
            };

            var url = _s3.GetPreSignedURL(req);

            return Task.FromResult(url);
        }

        

        
    }
}
