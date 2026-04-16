using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Infrastructure.Data;
using PhotosStorageMap.Infrastructure.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var summary = await _db.UploadCollections
                .Where(c => c.OwnerUserId == userId && !c.IsDeleted)
                .GroupBy(_ => 1)
                .Select(g => new UserStorageSummaryResponse(
                    g.Count(),
                    g.Sum(x => x.TotalPhotos),
                    g.Sum(x => x.TotalBytes),
                    g.Sum(x => x.TotalArchives),
                    g.Sum(x => x.TotalArchivesBytes),
                    g.Sum(x => x.TotalBytes + x.TotalArchivesBytes)
                    ))
                .SingleOrDefaultAsync(ct);

            return Ok(summary ?? new UserStorageSummaryResponse(
                TotalCollections: 0,
                TotalPhotos: 0,
                TotalPhotosBytes: 0,
                TotalArchives: 0,
                TotalArchivesBytes: 0,
                TotalStorageBytes: 0));
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
