using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Application.Interfaces;
using System.Collections.Concurrent;
using PhotosStorageMap.Domain.Enums;

namespace PhotosStorageMap.Infrastructure.Services
{
    public sealed class ZipJobStore : IZipJobStore
    {
        private readonly ConcurrentDictionary<Guid, ZipJobProgressDto> _jobs = new();

        public Guid Create(int totalFiles)
        {
            var jobId = Guid.NewGuid();

            _jobs[jobId] = new ZipJobProgressDto
            {
                JobId = jobId,
                TotalFiles = totalFiles,
                ProcessedFiles = 0,
                Status = ZipJobStatus.Running
            };

            return jobId;
        }

        public ZipJobProgressDto? Get(Guid jobId)
        {
            return _jobs.TryGetValue(jobId, out var job) ? job : null;
        }

        public void Update(Guid jobId, int processedFiles)
        {
            if (_jobs.TryGetValue(jobId, out var job))
            {
                job.ProcessedFiles = processedFiles;
            }
        }

        public void MarkReady(Guid jobId, string filePath, string fileName, string contentType)
        {
            if (_jobs.TryGetValue(jobId, out var job))
            {
                job.Status = ZipJobStatus.Ready;
                job.ProcessedFiles = job.TotalFiles;
                job.FilePath = filePath;
                job.FileName = fileName;
                job.ContentType = contentType;
            }
        }

        public void MarkFailed(Guid jobId, string error)
        {
            if ( _jobs.TryGetValue(jobId,out var job))
            {
                job.Status = ZipJobStatus.Failed;
                job.Error = error;
            }
        }        

        public void Remove(Guid jobId)
        {
            _jobs.TryRemove(jobId, out _);
        }

        
    }
}
