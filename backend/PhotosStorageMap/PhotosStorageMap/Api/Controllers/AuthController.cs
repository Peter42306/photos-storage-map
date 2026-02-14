using Google.Apis.Auth;
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

        // ----------------------------------------------------------------------
        // Register
        // ----------------------------------------------------------------------
        [HttpPost("register")]
        public async Task<ActionResult<MessageResponse>> Register(RegisterRequest request)
        {
            //var email = request.Email.Trim().ToLowerInvariant();
            var email = NormalizedEmail(request.Email);

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
                _logger.LogWarning(
                    "Registration failed for {Email}. Errors: {Codes}",
                    email,
                    string.Join(",", result.Errors.Select(e => e.Code)));

                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });
            }
            
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);                        
            var encodedToken = TokenEncoding.Encode(token);

            //var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            var confirmUrl = BuildFrontendUrl($"/confirm-email?userId={user.Id}&token={encodedToken}");            

            await _emailService.SendAsync(
                toEmail: email,
                subject: "Confirm your email",
                htmlBody: $"<p>Click to confirm your email:</p><p><a href=\"{confirmUrl}\">{confirmUrl}</a></p>"
                );

            _logger.LogInformation("User registered: {Email}. Confirmation link sent.", email);            

            return Ok(new MessageResponse("Registration successful. Please confirm your email."));
        }


        // ----------------------------------------------------------------------
        // Confirm email
        // ----------------------------------------------------------------------
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
                _logger.LogWarning(
                    "Email confirmation failed for user {UserId}. Errors: {Codes}",
                    user.Id,
                    string.Join(",", result.Errors.Select(e => e.Code)));

                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            _logger.LogInformation("Email confirmed successfully for user: {UserId}", user.Id);

            return Ok(new MessageResponse("Email confirmed."));
        }

        // ----------------------------------------------------------------------
        // Login
        // ----------------------------------------------------------------------
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            //var email = request.Email.Trim().ToLowerInvariant();
            var email = NormalizedEmail(request.Email);

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                return Unauthorized(new MessageResponse("Invalid email or password."));
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login blocked: inactive user {Email} (UserId={UserId})", email, user.Id);
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

            //user.LastLoginAt = DateTime.UtcNow;
            //user.LoginCount += 1;
            //await _userManager.UpdateAsync(user);
            await TouchLoginAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var (token, expiresAtUtc) = _jwt.CreateToken(user, roles);

            _logger.LogInformation("Login success for {Email} (UserId={UserId})", email, user.Id);
            return Ok(new AuthResponse(token, expiresAtUtc));
        }

        // ----------------------------------------------------------------------
        // Resend confirmation
        // ----------------------------------------------------------------------
        [HttpPost("resend-confirmation")]
        public async Task<ActionResult<MessageResponse>> ResendConfirmation(ResendConfirmationRequest request)
        {
            //var email = request.Email.Trim().ToLowerInvariant();
            var email = NormalizedEmail(request.Email);

            var user = await _userManager.FindByEmailAsync(email);
            
            if (user is null || !user.IsActive || user.EmailConfirmed)
            {
                _logger.LogInformation("Resend confirmation requested for {Email}. Generic response returned.", email);
                
                //return Ok(new MessageResponse("If an account exists, a confirmation email has been sent."));
                return Ok(GenericAccountExistsMessage());
            }


            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);            
            var encodedToken = TokenEncoding.Encode(token);

            //var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

            var confirmUrl = BuildFrontendUrl($"/confirm-email?userId={user.Id}&token={encodedToken}");

            await _emailService.SendAsync(
                toEmail: email,
                subject: "Confirm your email",
                htmlBody: $"<p>Click to confirm your email:</p><p><a href=\"{confirmUrl}\">{confirmUrl}</a></p>"
            );

            _logger.LogInformation("Resend confirmation link sent to: {Email}", email);

            //return Ok(new MessageResponse("If an account exists, a confirmation email has been sent."));
            return Ok(GenericAccountExistsMessage());
        }

        // ----------------------------------------------------------------------
        // Forgot password
        // ----------------------------------------------------------------------
        [HttpPost("forgot-password")]
        public async Task<ActionResult<MessageResponse>> ForgotPassword(ForgotPasswordRequest request)
        {
            //var email = request.Email.Trim().ToLowerInvariant();
            var email = NormalizedEmail(request.Email);

            _logger.LogInformation("Forgot password requested for: {Email}", email);

            var user = await _userManager.FindByEmailAsync(email);            

            if (user is null || !user.IsActive || !user.EmailConfirmed)
            {
                //return Ok(new MessageResponse("If an account exists, a reset email has been sent."));
                return Ok(GenericResetSentMessage());
            }
            
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = TokenEncoding.Encode(token);

            //var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            var resetUrl = BuildFrontendUrl($"/reset-password?userId={user.Id}&token={encodedToken}");

            await _emailService.SendAsync(
                toEmail: email,
                subject: "Reset your password",
                htmlBody: $"<p>Click to reset your password:</p><p><a href=\"{resetUrl}\">{resetUrl}</a></p>"
            );

            _logger.LogInformation("Password reset link sent to: {Email}", email);

            return Ok(GenericResetSentMessage());
        }

        // ----------------------------------------------------------------------
        // Reset password
        // ----------------------------------------------------------------------
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
                _logger.LogWarning(
                    "Password reset failed for user {UserId}. Errors: {Codes}",
                    user.Id,
                    string.Join(",", result.Errors.Select(e => e.Code)));

                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            _logger.LogInformation("Password reset successful for user: {UserId}", user.Id);

            return Ok(new MessageResponse("Password has been reset. You can sign in now."));

        }

        // ----------------------------------------------------------------------
        // Google login
        // ----------------------------------------------------------------------
        [HttpPost("google")]
        public async Task<ActionResult<AuthResponse>> Google([FromBody] GoogleLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
            {
                return BadRequest(new MessageResponse("idToken is required"));
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                var googleClientId = _configuration["Authentication:Google:ClientId"];
                if (string.IsNullOrWhiteSpace(googleClientId))
                {
                    _logger.LogError("Google login: ClientId not configured");
                    return StatusCode(500, new MessageResponse("Google ClientId is not configured"));
                }

                payload = await GoogleJsonWebSignature.ValidateAsync(
                    request.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { googleClientId }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google login: invalid token");
                return Unauthorized(new MessageResponse("Invalid Google token"));
            }


            var email = payload.Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Google login: no email in token (sub={Sub})", payload.Subject);
                return Unauthorized(new MessageResponse("Google account has no email"));
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = payload.Name?.Trim() ?? email,
                    EmailConfirmed = true,
                    IsActive = true,
                };

                var createRes = await _userManager.CreateAsync(user);
                if (!createRes.Succeeded)
                {
                    _logger.LogWarning(
                        "Google login: failed to create user {Email}. Errors: {Codes}",
                        email,
                        string.Join(",", createRes.Errors.Select(e => e.Code)));

                    return BadRequest(new MessageResponse("Failed to create user"));                    
                }

                _logger.LogInformation("Google login: new user created {Email} (UserId={UserId})", email, user.Id);
            }

            if (!user.IsActive)
            {                
                _logger.LogWarning("Google login: inactive user {Email} (UserId={UserId})", email, user.Id);
                return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse("User is inactive"));
            }

            //user.LastLoginAt = DateTime.UtcNow;
            //user.LoginCount += 1;
            //await _userManager.UpdateAsync(user);
            await TouchLoginAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var (token, expiresAtUtc) = _jwt.CreateToken(user, roles);

            _logger.LogInformation("Google login: success {Email} (UserId={UserId})", email, user.Id);
            return Ok(new AuthResponse(token, expiresAtUtc));
        }


        // ----------------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------------
        private bool IsConfirmedEmailRequired()
        {
            return _signInManager.Options.SignIn.RequireConfirmedEmail;
        }

        private string NormalizedEmail(string email)
        {
            return (email ?? string.Empty).Trim().ToLowerInvariant();
        }

        private string GetFrontendBaseUrl()
        {
            return _configuration["Frontend:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        }

        private string BuildFrontendUrl(string relativePath)
        {
            var baseUrl = GetFrontendBaseUrl().TrimEnd('/');
            var path = (relativePath ?? string.Empty).TrimStart('/');
            return $"{baseUrl}/{path}";
        }

        private MessageResponse GenericAccountExistsMessage()
        {
            return new MessageResponse("If an account exists, a confirmation email has been sent.");
        }

        private MessageResponse GenericResetSentMessage()
        {
            return new MessageResponse("If an account exists, a reset email has been sent.");
        }

        private async Task TouchLoginAsync(ApplicationUser user)
        {
            user.LastLoginAt = DateTime.UtcNow;
            user.LoginCount += 1;
            await _userManager.UpdateAsync(user);
        }

    }
}
