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
            if (!_jobs.TryGetValue(jobId, out var job))
            {
                return null;
            }

            lock (job)
            {
                return new ZipJobProgressDto
                {
                    JobId = job.JobId,
                    Status = job.Status,
                    ProcessedFiles = job.ProcessedFiles,
                    ArchivedFiles = job.ArchivedFiles,
                    TotalFiles = job.TotalFiles,
                    FailedFileNames = new List<string>(job.FailedFileNames),
                    Error = job.Error,
                    FilePath = job.FilePath,
                    FileName = job.FileName,
                    ContentType = job.ContentType
                };
            }            
        }

        public void Update(Guid jobId, int processedFiles, int archivedFiles, string? failedFileName = null)
        {
            if (_jobs.TryGetValue(jobId, out var job))
            {
                lock (job)
                {
                    job.ProcessedFiles = processedFiles;
                    job.ArchivedFiles = archivedFiles;

                    if (failedFileName is not null)
                    {
                        job.FailedFileNames.Add(failedFileName);
                    }
                }
                
            }
        }

        public void MarkReady(Guid jobId, string filePath, string fileName, string contentType)
        {
            if (_jobs.TryGetValue(jobId, out var job))
            {
                lock (job)
                {
                    job.Status = ZipJobStatus.Ready;                    
                    job.FilePath = filePath;
                    job.FileName = fileName;
                    job.ContentType = contentType;
                }                
            }
        }

        public void MarkFailed(Guid jobId, string error)
        {
            if ( _jobs.TryGetValue(jobId,out var job))
            {
                lock (job)
                {
                    job.Status = ZipJobStatus.Failed;
                    job.Error = error;
                }                
            }
        }        

        public void Remove(Guid jobId)
        {
            _jobs.TryRemove(jobId, out _);
        }

        
    }
}
