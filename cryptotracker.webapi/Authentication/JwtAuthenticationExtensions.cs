using System.Text;
using cryptotracker.core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace cryptotracker.webapi.Authentication;

public static class JwtAuthenticationExtensions
{
    public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection services, ICryptoTrackerConfig config)
    {
        var secretKey = Encoding.UTF8.GetBytes(config.Auth.Secret ?? throw new Exception("JWT Secret not configured"));
        if (secretKey.Length < 32)
            throw new Exception("JWT Secret must be at least 32 bytes (256 bits) for HMAC SHA256");

        var builder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        });

        builder.AddJwtBearer(jwtOptions =>
        {
            jwtOptions.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                ValidateIssuer = !string.IsNullOrWhiteSpace(config.Auth.Issuer),
                ValidIssuer = config.Auth.Issuer,
                ValidateAudience = !string.IsNullOrWhiteSpace(config.Auth.Audience),
                ValidAudience = config.Auth.Audience,
            };

            jwtOptions.Events = JwtAuthenticationEvents.Create();
        });

        return builder;
    }
}
