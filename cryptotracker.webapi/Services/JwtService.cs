using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using cryptotracker.core.Interfaces;
using cryptotracker.database.Models;
using Microsoft.IdentityModel.Tokens;

namespace cryptotracker.webapi.Services
{
    public class JwtService
    {
        private readonly ICryptoTrackerConfig _config;

        public JwtService(ICryptoTrackerConfig config)
        {
            _config = config;
        }

        public string GenerateJwtToken(ApplicationUser user)
        {
            var secretKey = Encoding.UTF8.GetBytes(_config.Auth.Secret!);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "user"),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
            };

            var key = new SymmetricSecurityKey(secretKey);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _config.Auth.Issuer ?? throw new Exception("JWT Issuer not configured"),
                audience: _config.Auth.Audience ?? throw new Exception("JWT Audience not configured"),
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_config.Auth.ExpiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public void SetJwtCookie(HttpResponse response, string jwt)
        {
            response.Cookies.Append("jwt", jwt, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(_config.Auth.ExpiryMinutes)
            });
        }
    }
}