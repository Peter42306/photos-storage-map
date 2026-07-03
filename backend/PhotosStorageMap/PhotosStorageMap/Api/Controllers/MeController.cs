using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Application.Common;
using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Infrastructure.Data;
using PhotosStorageMap.Infrastructure.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PhotosStorageMap.Domain.Enums;

namespace PhotosStorageMap.Api.Controllers
{
    [Route("api/me")]
    [ApiController]
    [Authorize]
    public class MeController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public MeController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Unauthorized();
            }

            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToArray();
            

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FullName,
                user.EmailConfirmed,
                roles
            });
        }

        [HttpGet("storage-summary")]
        public async Task<IActionResult> GetStorageSummary(CancellationToken ct)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var summary = await _db.UploadCollections
                .Where(c => c.OwnerUserId == user.Id && !c.IsDeleted)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalCollections = g.Count(),
                    TotalPhotos = g.Sum(x => x.TotalPhotos),
                    TotalPhotosBytes = g.Sum(x => x.TotalBytes),
                    TotalArchives = g.Sum(x => x.TotalArchives),
                    TotalArchivesBytes = g.Sum(x => x.TotalArchivesBytes),
                    TotalUsedStorageBytes = g.Sum(x => x.TotalBytes + x.TotalArchivesBytes),
                })
                .SingleOrDefaultAsync(ct);

            var totalCollections = summary?.TotalCollections ?? 0;
            var totalPhotos = summary?.TotalPhotos ?? 0;
            var totalPhotosBytes = summary?.TotalPhotosBytes ?? 0L;
            var totalArchives = summary?.TotalArchives ?? 0;
            var totalArchivesBytes = summary?.TotalArchivesBytes ?? 0L;
            var totalUsedStorageBytes = summary?.TotalUsedStorageBytes ?? 0L;

            var storagePlanLimitBytes = user.StoragePlan == StoragePlan.Pro
                ? Limits.UserStorage.MaxBytesPro
                : Limits.UserStorage.MaxBytesFree;

            var storageFreeBytes = Math.Max(0, storagePlanLimitBytes - totalUsedStorageBytes);

            var storageUsedPercent = storagePlanLimitBytes > 0
                ? Math.Round((double)totalUsedStorageBytes / storagePlanLimitBytes * 100, 1)
                : 0;

            return Ok(new UserStorageSummaryResponse(
                TotalCollections: totalCollections,
                TotalPhotos: totalPhotos,
                TotalPhotosBytes: totalPhotosBytes,
                TotalArchives: totalArchives,
                TotalArchivesBytes: totalArchivesBytes,
                TotalUsedStorageBytes: totalUsedStorageBytes,
                StoragePlan: user.StoragePlan.ToString(),
                StoragePlanLimitBytes: storagePlanLimitBytes,
                StorageFreeBytes: storageFreeBytes,
                StorageUsedPercent: storageUsedPercent));
        }

        //-------------------------------------------------------
        // Helper
        //-------------------------------------------------------
        private string? GetUserId()
        {
            return User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.Identity?.Name;
        }
    }
}
