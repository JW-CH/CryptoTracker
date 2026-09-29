using Microsoft.AspNetCore.Authentication;

namespace cryptotracker.webapi.Authentication;

public static class McpAuthenticationExtensions
{
    public static AuthenticationBuilder AddMcpAuthentication(this AuthenticationBuilder builder)
    {
        builder.AddScheme<AuthenticationSchemeOptions, McpAuthenticationHandler>(
            McpAuthenticationDefaults.Scheme,
            _ => { });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(McpAuthenticationDefaults.Policy, policy =>
            {
                policy.AddAuthenticationSchemes(McpAuthenticationDefaults.Scheme);
                policy.RequireAuthenticatedUser();
            });
        });

        return builder;
    }
}
