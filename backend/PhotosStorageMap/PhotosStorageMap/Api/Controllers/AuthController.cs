using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhotosStorageMap.Api.Services;
using PhotosStorageMap.Application.Common.Encoding;
using PhotosStorageMap.Application.DTOs.Auth;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Infrastructure.Identity;

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
            
            var encodedToken = TokenEncoding.Encode(token);

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
            if (user is null || !user.IsActive)
            {
                return BadRequest(new MessageResponse("Invalid confirmation."));
            }

            if (user.EmailConfirmed)
            {
                return Ok(new MessageResponse("Email confirmed."));
            }
            
            var decodedToken = TokenEncoding.Decode(token);

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);            

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            _logger.LogInformation("Email confirmed successfully for user: {UserId}", user.Id);

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
            
            if (user is null || !user.IsActive || user.EmailConfirmed)
            {
                _logger.LogInformation("Resend confirmation link sent to: {Email}, but email was already confirmed.", email);
                return Ok(new MessageResponse("If an account exists, a confirmation email has been sent."));
            }


            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            
            var encodedToken = TokenEncoding.Encode(token);

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


        [HttpPost("forgot-password")]
        public async Task<ActionResult<MessageResponse>> ForgotPassword(ForgotPasswordRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            _logger.LogInformation($"Forgot password: {email}");

            var user = await _userManager.FindByEmailAsync(email);
            _logger.LogInformation($"UserId: {user?.Id}, User email: {user?.Email}.");

            if (user is null || !user.IsActive || !user.EmailConfirmed)
            {
                return Ok(new MessageResponse("If an account exists, a reset email has been sent."));
            }
            
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = TokenEncoding.Encode(token);

            var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            var resetUrl = $"{frontendBaseUrl}/reset-password?userId={user.Id}&token={encodedToken}";

            await _emailService.SendAsync(
                toEmail: email,
                subject: "Reset your password",
                htmlBody: $"<p>Click to reset your password:</p><p><a href=\"{resetUrl}\">{resetUrl}</a></p>"
            );

            _logger.LogInformation("Password reset link sent to: {Email}", email);

            return Ok(new MessageResponse("If an account exists, a reset email has been sent."));
        }


        [HttpPost("reset-password")]
        public async Task<ActionResult<MessageResponse>> ResetPassword(ResetPasswordRequest request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user is null || !user.IsActive || !user.EmailConfirmed)
            {
                return BadRequest(new MessageResponse("Invalid reset request"));
            }

            var decodedToken = TokenEncoding.Decode(request.Token);

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            _logger.LogInformation("Password reset successful for user: {UserId}", user.Id);

            return Ok(new MessageResponse("Password has been reset. You can sign in now."));

        }

        // Helper
        private bool IsConfirmedEmailRequired()
        {
            return _signInManager.Options.SignIn.RequireConfirmedEmail;
        }

    }
}
