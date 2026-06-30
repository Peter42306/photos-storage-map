using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Infrastructure.Data;
using PhotosStorageMap.Infrastructure.Identity;

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
    }
}
