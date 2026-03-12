using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Infrastructure.Data;
using System.IO.Compression;
using PhotosStorageMap.Application.Common;
using System.Diagnostics;

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

        public Task<ArchiveBuildResult> BuildOriginalZipAsync(Guid collectionId, CancellationToken ct = default)
        {
            return BuildZipAsync(collectionId, ArchiveType.Original, ct);
        }

        public Task<ArchiveBuildResult> BuildStandardZipAsync(Guid collectionId, CancellationToken ct = default)
        {
            return BuildZipAsync(collectionId, ArchiveType.Standard, ct);
        }



        private async Task<ArchiveBuildResult> BuildZipAsync(
            Guid collectionId,
            ArchiveType type,
            CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            var collection = await _db.UploadCollections
                .FirstOrDefaultAsync(c => c.Id == collectionId);

            if (collection is null)
            {
                throw new InvalidOperationException("Collection not found.");
            }

            var photos = await _db.PhotoItems
                .Where(p => p.UploadCollectionId == collectionId)
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
            long totalBytes = 0;

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
                        continue;
                    }

                    try
                    {
                        await using var sourceStream = await _storage.OpenReadAsync(storageKey, ct);

                        var entryName = BuildEntryName(
                            photo.OriginalFileName,
                            type,
                            usedNames);

                        var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);

                        await using var entryStream = entry.Open();
                        await sourceStream.CopyToAsync(entryStream, ct);

                        filesCount++;

                        if (type == ArchiveType.Standard)
                        {
                            totalBytes += photo.StandardSizeBytes ?? 0;
                        }
                        else if (type == ArchiveType.Original)
                        {
                            totalBytes += photo.OriginalSizeBytes ?? 0;
                        }
                        else
                        {
                            _logger.LogWarning("ZIP ARCHIEVE: Size of PhotoId={PhotoId} was not caslculated", photo.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to add photo to zip. CollectionId={CollectionId}, PhotoId={PhotoId}, Type={Type}, Key={StorageKey}",
                            collectionId,
                            photo.Id,
                            type,
                            storageKey);

                        continue;
                    }

                    //_logger.LogInformation("ArchiveCollectionService: archieved PhotoId={PhotoId}, TotalFiles={TotalFiles}, TotalBytes={TotalBytes}",
                    //    photo.Id,
                    //    filesCount,
                    //    totalBytes);
                }
            };

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

            var suffix = type == ArchiveType.Standard ? ContentType.Standard : ContentType.Originals;
            var zipFileName = $"{safeCollectionTitle}_{suffix}.zip";

            sw.Stop();
            _logger.LogInformation(
                "ArchiveCollectionService: Archive build completed. CollectionId={Collectionid}, ArchiveType={Type}, TotalFiles={TotalFiles}, TotalBytes={TotalBytes}, Duration={Duration}",
                collectionId,
                type,
                filesCount,
                totalBytes,
                sw.ElapsedMilliseconds);

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
