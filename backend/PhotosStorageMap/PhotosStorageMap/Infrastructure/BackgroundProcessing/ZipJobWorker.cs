using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Application.Interfaces;

namespace PhotosStorageMap.Infrastructure.BackgroundProcessing
{
    public sealed class ZipJobWorker : BackgroundService
    {
        private readonly IZipJobQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IZipJobStore _zipJobStore;
        private readonly ILogger<ZipJobWorker> _logger;

        public ZipJobWorker(
            IZipJobQueue queue,
            IServiceScopeFactory scopeFactory,
            IZipJobStore zipJobStore,
            ILogger<ZipJobWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _zipJobStore = zipJobStore;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ZIP JOB WORKER started.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var request = await _queue.DequeueAsync(stoppingToken);

                    try
                    {
                        _logger.LogInformation(
                            "ZIP JOB WORKER : ZIP job {JobId} received for collection {CollectionId}",
                            request.JobId,
                            request.CollectionId);

                        await ProcessZipAsync(request, stoppingToken);


                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        _zipJobStore.MarkFailed(
                            request.JobId,
                            "ZIP JOB WORKER : ZIP processing interrupted by application shutdown.");

                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "ZIP JOB WORKER : ZIP job {JobId} failed.",
                            request.JobId);

                        _zipJobStore.MarkFailed(
                            request.JobId,
                            ex.Message);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("ZIP JOB WORKER stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(
                    ex,
                    "ZIP JOB WORKER terminated unexpectedly.");

                throw;
            }
        }

        private async Task ProcessZipAsync(ZipJobRequest request, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();

            var archiveService = scope.ServiceProvider.GetRequiredService<IArchiveCollectionService>();

            var result = await archiveService.BuildStandardZipAsync(
                request.CollectionId,
                request.JobId,
                _zipJobStore,
                ct);

            string filePath;

            await using (result.Stream)
            {
                if (result.Stream is not FileStream fileStream)
                {
                    throw new InvalidOperationException("ZIP result stream is not a Filestream.");
                }

                filePath = fileStream.Name;                
            }

            _zipJobStore.MarkReady(
                    request.JobId,
                    filePath,
                    result.FileName,
                    result.ContentType);

            _logger.LogInformation(
                "ZIP job {JobId} completed.",
                request.JobId);
        }
    }
}
