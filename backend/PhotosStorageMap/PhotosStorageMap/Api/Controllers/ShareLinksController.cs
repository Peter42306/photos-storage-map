using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Infrastructure.Data;
using System.Security.Claims;
using PhotosStorageMap.Domain.Entities;
using System.Net.WebSockets;
using Amazon.Runtime.CredentialManagement;

namespace PhotosStorageMap.Api.Controllers
{
    [Route("api/share-links")]
    [ApiController]
    [Authorize]
    public class ShareLinksController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ShareLinksController> _logger;

        public ShareLinksController(
            ApplicationDbContext db,
            ILogger<ShareLinksController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(
            [FromBody] CreateOrUpdateShareLinkRequest request,
            CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var collection = await _db.UploadCollections
                .Include(c => c.ShareLink)
                .FirstOrDefaultAsync(c => 
                c.Id == request.CollectionId &&
                c.OwnerUserId == userId &&
                !c.IsDeleted,
                ct);

            if (collection == null) return NotFound("Collection not found.");

            var token = Guid.NewGuid().ToString("N");

            if (collection.ShareLink is null)
            {
                collection.ShareLink = new ShareLink
                {
                    UploadCollectionId = collection.Id,
                    Token = token,
                    CreatedAtUtc = DateTime.UtcNow,
                    IsRevoked = false,
                    AllowSlideshowOriginals = request.AllowSlideshowOriginals,
                    AllowDownloadResizedZip = request.AllowDownloadResizedZip,
                    AllowDownloadOriginalFromCard = request.AllowDownloadOriginalFromCard
                };
            }
            else 
            {
                collection.ShareLink.Token = token;
                collection.ShareLink.CreatedAtUtc = DateTime.UtcNow;
                collection.ShareLink.IsRevoked = false;
                collection.ShareLink.AllowSlideshowOriginals = request.AllowSlideshowOriginals;
                collection.ShareLink.AllowDownloadOriginalFromCard = request.AllowDownloadOriginalFromCard;
            }

            await _db.SaveChangesAsync(ct);

            var shareLink = collection.ShareLink;
            var url = BuildShareLink(shareLink.Token);
            //var url = $"{Request.Scheme}://{Request.Host}/shared/{shareLink.Token}";

            return Ok(new ShareLinkResponse(
                shareLink.Id,
                shareLink.Token,
                url,
                shareLink.CreatedAtUtc,
                shareLink.IsRevoked,
                shareLink.AllowSlideshowOriginals,
                shareLink.AllowDownloadResizedZip,
                shareLink.AllowDownloadOriginalFromCard
            ));
        }

        [HttpGet("collection/{collectionId:guid}")]
        public async Task<IActionResult> GetByCollectionId(
            Guid collectionId,
            CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var shareLink = await _db.ShareLinks                
                .Include(s => s.UploadCollection)
                .SingleOrDefaultAsync(s => 
                    s.UploadCollectionId == collectionId &&
                    s.UploadCollection.OwnerUserId == userId &&
                    !s.UploadCollection.IsDeleted,
                    ct);

            if (shareLink == null) 
            {
                return Ok(null);
            }

            var url = BuildShareLink(shareLink.Token);

            return Ok(new ShareLinkResponse(
                shareLink.Id,
                shareLink.Token,
                url,
                shareLink.CreatedAtUtc,
                shareLink.IsRevoked,
                shareLink.AllowSlideshowOriginals,
                shareLink.AllowDownloadResizedZip,
                shareLink.AllowDownloadOriginalFromCard
            ));
        }

        [HttpPost("{id:guid}/revoke")]
        public async Task<IActionResult> Revoke(
            Guid id,
            CancellationToken ct)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var shareLink = await _db.ShareLinks
                .Include(s => s.UploadCollection)
                .SingleOrDefaultAsync(s =>
                    s.Id == id &&
                    s.UploadCollection.OwnerUserId == userId &&
                    !s.UploadCollection.IsDeleted,
                    ct);

            if (shareLink == null) return NotFound();

            shareLink.IsRevoked = true;

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

        private string BuildShareLink(string token)
        {
            return $"{Request.Scheme}://{Request.Host}/shared/{token}";
        }
    }
}
