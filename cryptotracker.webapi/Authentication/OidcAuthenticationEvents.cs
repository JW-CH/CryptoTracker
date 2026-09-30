using System.Security.Claims;
using cryptotracker.core.Interfaces;
using cryptotracker.database.Models;
using cryptotracker.webapi.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;

namespace cryptotracker.webapi.Authentication;

public static class OidcAuthenticationEvents
{
    public static OpenIdConnectEvents Create(ICryptoTrackerConfig config) => new()
    {
        OnTokenValidated = ctx => OnTokenValidated(ctx, config)
    };

    private static async Task OnTokenValidated(TokenValidatedContext ctx, ICryptoTrackerConfig config)
    {
        var userManager = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var email = ctx.Principal?.FindFirstValue(ClaimTypes.Email) ?? ctx.Principal?.FindFirst("email")?.Value ?? "";

        if (string.IsNullOrEmpty(email))
        {
            ctx.Fail("Email claim not found");
            return;
        }

        var jwtService = ctx.HttpContext.RequestServices.GetRequiredService<JwtService>();
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            if (!config.Oidc.AutoProvision)
            {
                ctx.Fail($"User {email} is not provisioned and oidc auto provisioning is disabled");
                return;
            }

            user = new ApplicationUser { Email = email, UserName = email, EmailConfirmed = true };
            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                ctx.Fail("User creation failed");
                return;
            }
        }

        var jwt = jwtService.GenerateJwtToken(user, ctx.Request);
        jwtService.SetJwtCookie(ctx.Response, jwt);
    }
}
