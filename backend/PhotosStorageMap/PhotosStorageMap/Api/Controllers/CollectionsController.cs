using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Domain.Entities;
using PhotosStorageMap.Infrastructure.Data;
using System.Security.Claims;

namespace PhotosStorageMap.Api.Controllers
{
    [Route("api/collections")]
    [ApiController]
    public sealed class CollectionsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CollectionsController> _logger;

        public CollectionsController(
            ApplicationDbContext db,
            ILogger<CollectionsController> logger)
        {
            _db = db;
            _logger = logger;
        }


        [HttpPost]
        public async Task<ActionResult<Guid>> CreateCollection(CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var collection = new UploadCollection
            {
                OwnerUserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.UploadCollections.Add(collection);
            await _db.SaveChangesAsync();

            return Ok(collection.Id);
        }



        [HttpGet]
        public async Task<ActionResult> GetMyCollections(CancellationToken ct)
        {
            var userId = GetUserId();
            if(string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var collections = await _db.UploadCollections
                .Where(x => x.OwnerUserId == userId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Description,
                    x.CreatedAtUtc,
                    //x.ExpiresAtUtc,
                    x.TotalPhotos,
                    x.TotalBytes

                })
                .ToListAsync(ct);

            return Ok(collections);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteCollection(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var collection = await _db.UploadCollections
                .FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == userId);

            if (collection is null) return NotFound();

            _db.UploadCollections.Remove(collection);
            await _db.SaveChangesAsync(ct);

            return NoContent();
        }


        //-------------------------------------------------------
        // Helper
        //-------------------------------------------------------
        private string? GetUserId()
        {
            return User.FindFirstValue("sub")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.Identity?.Name;
        }
    }
}
