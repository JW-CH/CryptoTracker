using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace cryptotracker.webapi.Authentication;

public static class JwtAuthenticationEvents
{
    public static JwtBearerEvents Create() => new()
    {
        OnMessageReceived = OnMessageReceived
    };

    private static Task OnMessageReceived(MessageReceivedContext context)
    {
        // Prefer Bearer header; fall back to cookie
        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            var cookie = context.Request.Cookies["jwt"];
            if (!string.IsNullOrEmpty(cookie))
                context.Token = cookie;
        }

        return Task.CompletedTask;
    }
}
