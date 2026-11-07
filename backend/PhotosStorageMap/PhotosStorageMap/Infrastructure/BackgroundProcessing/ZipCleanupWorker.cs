using PhotosStorageMap.Application.Common;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Domain.Enums;

namespace PhotosStorageMap.Infrastructure.BackgroundProcessing
{
    public sealed class ZipCleanupWorker : BackgroundService
    {
        private readonly IZipJobStore _zipJobStore;
        private readonly ILogger<ZipCleanupWorker> _logger;

        private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(Limits.ZipCleanupWorker.CleanupInterval);
        private static readonly TimeSpan ZipLifetime = TimeSpan.FromHours(Limits.ZipCleanupWorker.ZipLifeTime);

        public ZipCleanupWorker(
            IZipJobStore zipJobStore,
            ILogger<ZipCleanupWorker> logger)
        {
            _zipJobStore = zipJobStore;
            _logger = logger;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {            
            _logger.LogInformation("ZIP CLEANUP WORKER started.");

            try
            {
                Cleanup();

                using var timer = new PeriodicTimer(CleanupInterval);

                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    Cleanup();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal application shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "ZIP CLEANUP WORKER : ZIP Cleanup Worker terminated unexpectedly.");
            }
            finally
            {
                _logger.LogInformation("ZIP CLEANUP WORKER stopped.");
            }            
        }


        private void Cleanup()
        {
            

            try
            {
                var now = DateTime.UtcNow;
                var jobs = _zipJobStore.GetAll();

                // 1. Remove expired completed jobs
                foreach (var job in jobs)
                {
                    if (job.Status == ZipJobStatus.Running ||
                        job.CompletedAtUtc is null ||
                        now - job.CompletedAtUtc.Value < ZipLifetime)
                    {
                        continue;
                    }

                    if (job.FilePath is not null)
                    {
                        TryDeleteFile(job.FilePath);
                    }

                    _zipJobStore.Remove(job.JobId);
                }

                // Cleanup of ZIP remaining after server shutdown
                var activePaths = _zipJobStore.GetAll()
                    .Where(j => j.FilePath is not null)
                    .Select(j => Path.GetFullPath(j.FilePath!))
                    .ToHashSet(StringComparer.Ordinal);

                var tempDirectory = Path.GetTempPath();

                foreach (var filePath in Directory.EnumerateFiles(tempDirectory, "photos-storage-map_*.zip"))
                {
                    if (activePaths.Contains(Path.GetFullPath(filePath)))
                    {
                        continue;
                    }

                    try
                    {
                        var lastModified = File.GetLastWriteTimeUtc(filePath);

                        if (now - lastModified >= ZipLifetime)
                        {
                            TryDeleteFile(filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "ZIP CLEANUP WORKER : Could not inspect ZIP file {FilePath}",
                            filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "ZIP CLEANUP WORKER : ZIP cleanup cycle failed.");
            }
            
        }

        private void TryDeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);

                    _logger.LogInformation(
                        "ZIP CLEANUP WORKER : Deleted expired ZIP: {FilePath}",
                        filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "ZIP CLEANUP WORKER : Could not delete ZIP file {FilePath}",
                    filePath);
            }
        }
    }
}
