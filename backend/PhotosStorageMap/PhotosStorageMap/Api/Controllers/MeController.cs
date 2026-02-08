using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhotosStorageMap.Infrastructure.Identity;
using System.Security.Claims;

namespace PhotosStorageMap.Api.Controllers
{
    [Route("api/me")]
    [ApiController]
    [Authorize]
    public class MeController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public MeController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
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
    }
}
