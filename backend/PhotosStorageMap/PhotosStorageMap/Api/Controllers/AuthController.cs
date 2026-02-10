using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhotosStorageMap.Api.Services;
using PhotosStorageMap.Application.DTOs.Auth;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Infrastructure.Identity;
using System.Net;

namespace PhotosStorageMap.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtTokenService _jwt;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            JwtTokenService jwt,
            IEmailService emailService,
            ILogger<AuthController> logger,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwt = jwt;
            _emailService = emailService;
            _logger = logger;
            _configuration=configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<MessageResponse>> Register(RegisterRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                return Conflict(new MessageResponse("Email is already in use"));
            }

            var user = new ApplicationUser
            {
                Email = email,
                UserName = email,
                FullName = request.FullName?.Trim()
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            // TODO - Generate confirm email token and log link (later will send via SendGrid)
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);

            var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

            var confirmUrl = $"{frontendBaseUrl}/confirm-email?userId={user.Id}&token={encodedToken}";
            //var confirmUrl = $"{frontendBaseUrl}/confirm-email?userId={user.Id}&token={token}";            

            await _emailService.SendAsync(
                toEmail: email,
                subject: "Confirm your email",
                htmlBody: $"<p>Click to confirm your email:</p><p><a href=\"{confirmUrl}\">{confirmUrl}</a></p>"
                );

            _logger.LogInformation("User registered: {Email}. Confirmation link sent.", email);

            return Ok(new MessageResponse("Registration successful. Please confirm your email."));
        }


        [HttpGet("confirm-email")]
        public async Task<ActionResult<MessageResponse>> ConfirmEmail(
            [FromQuery] string userId,
            [FromQuery] string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return BadRequest(new MessageResponse("Invalid confirmation."));
            }

            //var decodedToken = WebUtility.UrlDecode(token);
            //var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            return Ok(new MessageResponse("Email confirmed."));
        }


        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                return Unauthorized(new MessageResponse("Invalid credentials."));
            }

            if (!user.IsActive)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse("User is inactive"));
            }

            if (IsConfirmedEmailRequired() && !user.EmailConfirmed)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse("Please confirm your email."));
            }

            var signIn = await _signInManager.CheckPasswordSignInAsync(
                user,
                request.Password,
                lockoutOnFailure: true);

            if (!signIn.Succeeded)
            {
                return Unauthorized(new MessageResponse("Invalid credentials."));
            }

            user.LastLoginAt = DateTime.UtcNow;
            user.LoginCount += 1;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var (token, expiresAtUtc) = _jwt.CreateToken(user, roles);

            return Ok(new AuthResponse(token, expiresAtUtc));
        }




        // Helper
        private bool IsConfirmedEmailRequired()
        {
            return _signInManager.Options.SignIn.RequireConfirmedEmail;
        }
    }
}
