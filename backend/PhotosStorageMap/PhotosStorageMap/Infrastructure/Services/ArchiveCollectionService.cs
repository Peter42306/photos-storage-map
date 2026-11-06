using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Infrastructure.Data;
using System.IO.Compression;
using PhotosStorageMap.Application.Common;
using System.Diagnostics;
using PhotosStorageMap.Domain.Enums;

namespace PhotosStorageMap.Infrastructure.Services
{
    public class ArchiveCollectionService : IArchiveCollectionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileStorage _storage;
        private readonly ILogger<ArchiveCollectionService> _logger;

        public ArchiveCollectionService(
            ApplicationDbContext db,
            IFileStorage storage,
            ILogger<ArchiveCollectionService> logger)
        {
            _db = db;
            _storage = storage;
            _logger = logger;
        }

        public Task<ArchiveBuildResult> BuildOriginalZipAsync(
            Guid collectionId, 
            CancellationToken ct = default)
        {
            return BuildZipAsync(
                collectionId, 
                ArchiveType.Original, 
                ct);
        }

        public Task<ArchiveBuildResult> BuildStandardZipAsync(
            Guid collectionId, 
            Guid? jobId, 
            IZipJobStore? zipJobStore, 
            CancellationToken ct = default)
        {
            return BuildZipAsync(
                collectionId, 
                ArchiveType.Standard, 
                ct, 
                jobId, 
                zipJobStore);
        }

        public Task<ArchiveBuildResult> BuildStandardZipAsync(
            Guid collectionId, 
            CancellationToken ct = default)
        {
            return BuildZipAsync(
                collectionId, 
                ArchiveType.Standard, ct);
        }


        

        private async Task<ArchiveBuildResult> BuildZipAsync(
            Guid collectionId,
            ArchiveType type,
            CancellationToken ct,
            Guid? jobId = null,
            IZipJobStore? zipJobStore = null)
        {
            var sw = Stopwatch.StartNew();

            var collection = await _db.UploadCollections
                .FirstOrDefaultAsync(c => c.Id == collectionId, ct);

            if (collection is null)
            {
                throw new InvalidOperationException("Collection not found.");
            }

            var photos = await _db.PhotoItems
                .Where(p => 
                    p.UploadCollectionId == collectionId && 
                    p.Status == PhotoStatus.Ready &&
                    p.StandardKey != null &&
                    p.StandardKey != "")
                .OrderBy(p => p.CreatedAtUtc)
                .ToListAsync(ct);

            if (photos.Count == 0)
            {
                throw new InvalidOperationException("No photos found in collection.");
            }

            _logger.LogInformation(
                "ArchiveCollectionService: Archive build started. CollectionId={Collectionid}, ArchiveType={Type}",
                collectionId,
                type);

            var tempZipPath = Path.Combine(
                Path.GetTempPath(),
                $"photos-storage-map_{collectionId}_{type}_{Guid.NewGuid():N}.zip");

            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var filesCount = 0;
            var processedFiles = 0;
            long totalBytes = 0;

            try
            {
                await using (var fileStream = new FileStream(
                tempZipPath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None))
                {
                    using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen: true);

                    foreach (var photo in photos)
                    {
                        ct.ThrowIfCancellationRequested();

                        var storageKey = type == ArchiveType.Standard
                            ? photo.StandardKey
                            : photo.OriginalKey;

                        if (string.IsNullOrWhiteSpace(storageKey))
                        {
                            processedFiles++;

                            if (jobId.HasValue && zipJobStore is not null)
                            {
                                zipJobStore.Update(jobId.Value, processedFiles,filesCount,photo.OriginalFileName ?? "photo.jpg");
                            }

                            _logger.LogWarning(
                                "Photo skipped: storage key is empty. PhotoId={PhotoId}",
                                photo.Id);

                            continue;
                        }

                        Stream sourceStream;

                        // Trying to open photo from S3
                        try
                        {
                            sourceStream = await _storage.OpenReadAsync(storageKey, ct);
                        }
                        catch (OperationCanceledException) when (ct.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            processedFiles++;

                            if (jobId.HasValue && zipJobStore is not null)
                            {
                                zipJobStore.Update(jobId.Value, processedFiles, filesCount, photo.OriginalFileName ?? "photo.jpg");
                            }

                            _logger.LogWarning(
                                ex,
                                "Failed to open photo from storage. PhotoId={PhotoId},Key={StorageKey}",
                                photo.Id,
                                storageKey);

                            continue;
                        }

                        // Photo is opened -> adding it to ZIP
                        await using (sourceStream)
                        {
                            var entryName = BuildEntryName(photo.OriginalFileName, type, usedNames);

                            var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);

                            await using (var entryStream = entry.Open())
                            {
                                await sourceStream.CopyToAsync(entryStream, ct);
                            }

                            filesCount++;
                            processedFiles++;

                            if (jobId.HasValue && zipJobStore is not null)
                            {
                                zipJobStore.Update(jobId.Value, processedFiles, filesCount);
                            }

                            _logger.LogInformation(
                                "Archived photo to ZIP. FileNo={FileCount}, PhotoId={PhotoId}",
                                filesCount,
                                photo.Id);

                            if (type == ArchiveType.Standard)
                            {
                                totalBytes += photo.StandardSizeBytes ?? 0;
                            }
                            else if (type == ArchiveType.Original)
                            {
                                totalBytes += photo.OriginalSizeBytes ?? 0;
                            }
                        }                        
                    }
                }                
            }
            catch
            {
                try
                {
                    if (File.Exists(tempZipPath))
                    {
                        File.Delete(tempZipPath);
                        _logger.LogInformation("Partial ZIP deleted. Path={TempZipPath}", tempZipPath);
                    }
                }
                catch (Exception cleanupEx)
                {

                    _logger.LogWarning(cleanupEx, "Failed to delete partial ZIP. Path={Path}", tempZipPath);
                }

                throw;
            }            
            

            if (filesCount == 0)
            {
                try
                {
                    if (File.Exists(tempZipPath))
                    {
                        File.Delete(tempZipPath);
                    }
                }
                catch
                {
                }

                throw new InvalidOperationException("No files available for archive.");
            }

            var resultStream = new FileStream(
                tempZipPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            var safeCollectionTitle = string.IsNullOrWhiteSpace(collection.Title)
                ? "collection"
                : MakeSafeFileName(collection.Title);

            var suffix = type == ArchiveType.Standard ? ContentType.Standard : ContentType.Original;
            var zipFileName = $"{safeCollectionTitle}_{suffix}.zip";

            sw.Stop();

            _logger.LogInformation(
                "ArchiveCollectionService: Archive build completed. CollectionId={Collectionid}, ArchiveType={Type}, TotalFiles={TotalFiles}, TotalBytes={TotalBytes}, Duration={Minutes} m {Seconds} s",
                collectionId,
                type,
                filesCount,
                totalBytes,
                sw.Elapsed.Minutes,
                sw.Elapsed.Seconds);

            return new ArchiveBuildResult(
                resultStream,
                zipFileName,
                ContentType.ApplicationZip,
                filesCount,
                totalBytes);
        }

        // Helpers

        private static string BuildEntryName(
            string? originalFileName,
            ArchiveType type,
            HashSet<string> usedNames)
        {
            var safeName = string.IsNullOrWhiteSpace(originalFileName)
                ? "photo.jpg" 
                : MakeSafeFileName(originalFileName);

            string proposedName;

            if (type == ArchiveType.Standard)
            {
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(safeName);
                proposedName = $"{nameWithoutExtension}_{ContentType.Standard}.jpg";
            }
            else
            {
                proposedName= safeName;
            }

            var uniqueName = proposedName;
            var counter = 2;

            while (!usedNames.Add(uniqueName))
            {
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(proposedName);
                var extension = Path.GetExtension(proposedName);

                uniqueName = $"{nameWithoutExtension}_{counter}{extension}";
                counter++;
            }

            return uniqueName;
        }

        private static string MakeSafeFileName(string value)
        {
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidChar, '_');
            }

            return value.Trim();
        }
        

        private enum ArchiveType
        {
            Standard = 1,
            Original = 2
        }
    }
}
