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

        public string GenerateJwtToken(ApplicationUser user, HttpRequest request)
        {
            if (string.IsNullOrEmpty(user.Email))
                throw new ArgumentException("User email is required for JWT generation", nameof(user));

            var issuer = GetIssuer(request);
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
                issuer: issuer,
                audience: issuer,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_config.Auth.ExpiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GetIssuer(HttpRequest request)
        {
            if (!string.IsNullOrWhiteSpace(_config.Auth.Issuer))
                return _config.Auth.Issuer;

            return $"{request.Scheme}://{request.Host}";
        }

        public void SetJwtCookie(HttpResponse response, string jwt)
        {
            var isHttps = response.HttpContext.Request.Scheme == "https";
            response.Cookies.Append("jwt", jwt, new CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(_config.Auth.ExpiryMinutes)
            });
        }
    }
}