using PhotosStorageMap.Application.Interfaces;

namespace PhotosStorageMap.Infrastructure.Storage
{
    public sealed class DiskFileStorage : IFileStorage
    {
        private readonly string _basePath;

        public DiskFileStorage(IConfiguration configuration)
        {
            var basePath = configuration["Storage:BasePath"]
                ?? throw new InvalidOperationException("Storage:BasePath is missing.");

            _basePath = Path.GetFullPath(basePath);
            Directory.CreateDirectory(_basePath);
        }


        public async Task<string> PutAsync(FileSaveRequest request, CancellationToken ct = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.StorageKey))
            {
                throw new ArgumentException("Storage key is required", nameof(request));
            }

            if (request.Content is null)
            {
                throw new ArgumentException("Content is required", nameof(request));
            }

            ct.ThrowIfCancellationRequested();

            var safeKey = NormalizeKey(request.StorageKey);
            var fullPath = GetSafeFullPath(safeKey);

            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (request.Content.CanSeek)
            {
                request.Content.Position = 0;
            }

            // atomic write
            var tempPath = fullPath + ".tmp_" + Guid.NewGuid().ToString("N");

            try
            {
                await using (var fs = new FileStream(
                    tempPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 64*1024,
                    useAsync: true))
                {
                    await request.Content.CopyToAsync(fs, ct);
                    await fs.FlushAsync(ct);
                }

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                File.Move(tempPath, fullPath);

                return safeKey;
            }
            catch
            {
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                    //ignore cleanup errors
                }
                throw;
            }
        }


        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
            {
                throw new ArgumentException("Storage key is required.", nameof(storageKey));
            }

            ct.ThrowIfCancellationRequested();

            var safekey = NormalizeKey(storageKey);
            var fullPath = GetSafeFullPath(safekey);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {safekey}", fullPath);
            }

            Stream stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64*1024,
                useAsync: true);

            return Task.FromResult(stream);
        }

        
        public Task<bool> DeleteAsync(string storageKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
            {
                return Task.FromResult(false);
            }

            ct.ThrowIfCancellationRequested();

            var safeKey = NormalizeKey(storageKey);
            var fullpath = GetSafeFullPath(safeKey);

            if (!File.Exists(fullpath))
            {
                return Task.FromResult(false);
            }

            File.Delete(fullpath);
            return Task.FromResult(true);
        }

        
        // --------------------------------------------------
        // Helpers
        // --------------------------------------------------

        private static string NormalizeKey(string storageKey)
        {
            var key = storageKey.Replace('\\','/').Trim().TrimStart('/');

            if (key.Length == 0)
            {
                throw new InvalidOperationException("Invalid storage key.");
            }

            var segments = key.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Any(s => s == "." || s ==".."))
            {
                throw new InvalidOperationException("Invalid storage key");
            }

            var normalizedKey = string.Join("/", segments);

            return normalizedKey;
        }

        private string GetSafeFullPath(string safeKey)
        {
            var combined = Path.Combine(_basePath, safeKey.Replace('/', Path.DirectorySeparatorChar));
            var fullPath = Path.GetFullPath(combined);

            // path traversal
            if (!fullPath.StartsWith(_basePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(fullPath, _basePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid storage key.");
            }

            return fullPath;
        }
    }
}
