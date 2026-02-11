using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using PhotosStorageMap.Api.Services;
using PhotosStorageMap.Application.DTOs.Auth;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Infrastructure.Identity;
using System.Net;
using System.Text;

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
            _configuration = configuration;
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
            
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);            
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            var confirmUrl = $"{frontendBaseUrl}/confirm-email?userId={user.Id}&token={encodedToken}";            

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
            
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);            

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
                return Unauthorized(new MessageResponse("Invalid email or password."));
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
                return Unauthorized(new MessageResponse("Invalid email or password."));
            }

            user.LastLoginAt = DateTime.UtcNow;
            user.LoginCount += 1;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var (token, expiresAtUtc) = _jwt.CreateToken(user, roles);

            return Ok(new AuthResponse(token, expiresAtUtc));
        }

        [HttpPost("resend-confirmation")]
        public async Task<ActionResult<MessageResponse>> ResendConfirmation(ResendConfirmationRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var user = await _userManager.FindByEmailAsync(email);

            // If not exists, we do not show explicitly
            if (user is null)
            {
                return Ok(new MessageResponse("If an account exists, a confirmation email has been sent."));
            }

            if (user.EmailConfirmed)
            {
                return Ok(new MessageResponse("If an account exists, a confirmation email has been sent."));
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            var confirmUrl = $"{frontendBaseUrl}/confirm-email?userId={user.Id}&token={encodedToken}";

            await _emailService.SendAsync(
                toEmail: email,
                subject: "Confirm your email",
                htmlBody: $"<p>Click to confirm your email:</p><p><a href=\"{confirmUrl}\">{confirmUrl}</a></p>"
            );

            _logger.LogInformation("Resend confirmation link sent to: {Email}", email);

            return Ok(new MessageResponse("If an account exists, a confirmation email has been sent."));
        }




        // Helper
        private bool IsConfirmedEmailRequired()
        {
            return _signInManager.Options.SignIn.RequireConfirmedEmail;
        }
    }
}
