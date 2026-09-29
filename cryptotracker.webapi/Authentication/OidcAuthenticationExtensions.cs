using cryptotracker.core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace cryptotracker.webapi.Authentication;

public static class OidcAuthenticationExtensions
{
    public static AuthenticationBuilder AddOidcAuthentication(this AuthenticationBuilder builder, ICryptoTrackerConfig config)
    {
        builder
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, oidcOptions =>
            {
                oidcOptions.Authority = config.Oidc.Authority;
                oidcOptions.ClientId = config.Oidc.ClientId;
                oidcOptions.ClientSecret = config.Oidc.ClientSecret;
                oidcOptions.ResponseType = OpenIdConnectResponseType.Code;
                oidcOptions.CallbackPath = "/api/signin-oidc";

                oidcOptions.Scope.Clear();
                oidcOptions.Scope.Add("openid");
                oidcOptions.Scope.Add("profile");
                oidcOptions.Scope.Add("email");

                oidcOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                oidcOptions.Events = OidcAuthenticationEvents.Create(config);
            });

        return builder;
    }
}
