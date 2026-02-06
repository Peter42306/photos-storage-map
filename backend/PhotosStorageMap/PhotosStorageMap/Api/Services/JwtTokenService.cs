using PhotosStorageMap.Infrastructure.Identity;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PhotosStorageMap.Api.Services
{
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public (string token, DateTime expireAtUtc) CreateToken(
            ApplicationUser user,
            IList<string> roles)
        {
            var jwt = _configuration.GetSection("Jwt");

            var issuer = jwt["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is missing");
            var audience = jwt["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is missing");
            var key = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is missing");


            var expiresMinutesRaw = jwt["ExpiresMinutes"];
            var expiresMinutes = 60;
            
            if (!string.IsNullOrWhiteSpace(expiresMinutesRaw) && int.TryParse(expiresMinutesRaw, out var parsed))
            {
                expiresMinutes = parsed;
            }

            var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiresMinutes);

            var claims = new List<Claim> 
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
            };

            foreach (var role in roles) 
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAtUtc,
                signingCredentials: creds
            );

            var jwtString = new JwtSecurityTokenHandler().WriteToken(token);
            return (jwtString, expiresAtUtc);

        }
    }
}
