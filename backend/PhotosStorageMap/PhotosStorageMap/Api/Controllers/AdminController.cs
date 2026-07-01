using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Infrastructure.Data;
using PhotosStorageMap.Infrastructure.Identity;
using System.Security.Claims;

namespace PhotosStorageMap.Api.Controllers
{    
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = RoleNames.Admin)]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("users")]
        public async Task<ActionResult<List<AdminUserSummaryDto>>> GetUsers(CancellationToken ct)
        {
            var users = await _db.Users
                .AsNoTracking()
                .Select(u => new AdminUserSummaryDto
                {
                    UserId = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    IsActive = u.IsActive,
                    EmailConfirmed = u.EmailConfirmed,
                    StoragePlan = u.StoragePlan,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    LoginCount = u.LoginCount,
                    AdminNote = u.AdminNote,

                    CollectionsCount = _db.UploadCollections
                        .Count(c => c.OwnerUserId == u.Id && !c.IsDeleted),

                    PhotosCount = _db.UploadCollections
                        .Where(c => c.OwnerUserId == u.Id && !c.IsDeleted)
                        .Sum(c => (int?)c.TotalPhotos) ?? 0,

                    ArchivesCount = _db.UploadCollections
                        .Where(c => c.OwnerUserId == u.Id && !c.IsDeleted)
                        .Sum(c => (int?)c.TotalArchives) ?? 0,

                    PhotosBytes = _db.UploadCollections
                        .Where(c => c.OwnerUserId == u.Id && !c.IsDeleted)
                        .Sum(c => (long?)c.TotalBytes) ?? 0,

                    ArchivesBytes = _db.UploadCollections
                        .Where(c => c.OwnerUserId == u.Id && !c.IsDeleted)
                        .Sum(c => (long?)c.TotalArchivesBytes) ?? 0,

                    TotalStorageBytes = _db.UploadCollections
                        .Where(c => c.OwnerUserId == u.Id && !c.IsDeleted)
                        .Sum(c => (long?)(c.TotalBytes + c.TotalArchivesBytes)) ?? 0

                })
                .OrderByDescending(u => u.TotalStorageBytes)
                .ToListAsync(ct);

            return Ok(users);
        }

        [HttpPatch("users/{userId}/active")]
        public async Task<IActionResult> UpdateUserActive(
            string userId,
            [FromBody] UpdateUserActiveRequest request,
            CancellationToken ct)
        {
            var currentUserId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == userId && request.IsActive == false)
            {
                return BadRequest("You cannot deactivate your own admin account.");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null)
            {
                return NotFound();
            }            

            user.IsActive = request.IsActive;

            await _db.SaveChangesAsync(ct);

            return NoContent();
        }

        [HttpPatch("users/{userId}/storage-plan")]
        public async Task<IActionResult> UpdateUserStoragePlan(
            string userId,
            [FromBody] UpdateUserStoragePlanRequest request,
            CancellationToken ct)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null)
            {
                return NotFound();
            }

            if (!Enum.IsDefined(request.StoragePlan))
            {
                return BadRequest("Invalid storage plan.");
            }

            user.StoragePlan = request.StoragePlan;

            await _db.SaveChangesAsync(ct);

            return NoContent();
        }
    }
}
