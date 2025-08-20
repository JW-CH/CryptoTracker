using System.Security.Claims;
using System.Text.Json.Serialization;
using cryptotracker.core.Interfaces;
using cryptotracker.core.Logic;
using cryptotracker.core.Models;
using cryptotracker.webapi.auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

var config = GetConfig(builder);

LogLevel level = LogLevel.Information;
if (!string.IsNullOrWhiteSpace(config.LogLevel))
{
    Enum.TryParse(config.LogLevel, true, out level);
}

builder.Services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(level);
            builder.AddSimpleConsole(options =>
                    {
                        // Customizing the log output format
                        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";  // Custom timestamp format
                        options.SingleLine = true;
                    });


            // Disable EF Core info logs
            builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            // Disable SpaProxy info logs
            builder.AddFilter("Microsoft.AspNetCore.SpaProxy", LogLevel.Warning);
            // Disable AspNetCore info logs
            builder.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        });

builder.Services.AddSingleton<ICryptotrackerConfig>(srv =>
{
    return config;
});

builder.Services.AddSingleton(srv =>
{
    var logger = srv.GetRequiredService<ILogger<CryptoTrackerLogic>>();
    return new CryptoTrackerLogic(logger);
});

builder.Services.AddSingleton<IFiatLogic>(srv =>
{
    var logger = srv.GetRequiredService<ILogger<FiatLogic>>();
    return new FiatLogic(logger);
});

builder.Services.AddSingleton<IStockLogic>(srv =>
{
    var logger = srv.GetRequiredService<ILogger<EmptyStockLogic>>();
    var fiatLogic = srv.GetRequiredService<IFiatLogic>();
    var config = srv.GetRequiredService<ICryptotrackerConfig>();

    if (string.IsNullOrWhiteSpace(config?.StockApi))
    {
        return new EmptyStockLogic(logger);
    }
    return new YahooFinanceStockLogic(logger, fiatLogic);
});

builder.Services.AddDbContext<DatabaseContext>((serviceProvider, options) =>
{
    var config = serviceProvider.GetRequiredService<ICryptotrackerConfig>();
    var connectionString = config?.ConnectionString ?? "";
    options.UseMySQL(connectionString).LogTo(Console.WriteLine, LogLevel.Warning);
    options.EnableSensitiveDataLogging(false);
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddScoped<IUserProvisioningService, UserProvisioningService>();

if (config.Oidc == null)
{
    throw new Exception("OIDC configuration is missing in the config file.");
}

var authBuilder = builder.Services
  .AddAuthentication(options =>
  {
      options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
      options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
  })
  .AddCookie()
  .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
  {
      options.GetClaimsFromUserInfoEndpoint = true;

      options.Events = new OpenIdConnectEvents
      {
          OnTokenValidated = async ctx =>
          {
              // Provision user in a scoped service
              var svc = ctx.HttpContext.RequestServices.GetRequiredService<IUserProvisioningService>();
              await svc.UpsertFromOidcAsync(ctx.Principal!);
          }
      };
  });

// Update OIDC config with our custom options
authBuilder.Services.AddOptions<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme)
    .Configure<ICryptotrackerConfig>((oidcOptions, config) =>
    {
        if (config.Oidc == null)
        {
            throw new Exception("OIDC configuration is missing in the config file.");
        }

        oidcOptions.Authority = config.Oidc.Authority;
        oidcOptions.ClientId = config.Oidc.ClientId;
        oidcOptions.ClientSecret = config.Oidc.ClientSecret;
        oidcOptions.ResponseType = OpenIdConnectResponseType.Code;

        oidcOptions.Scope.Clear();
        oidcOptions.Scope.Add("openid");
        oidcOptions.Scope.Add("profile");
        oidcOptions.Scope.Add("email");
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<UpdateService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    db.Database.Migrate(); // apply apply migrations to database
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsProduction())
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

// 3) Login endpoint → triggers OIDC challenge
app.MapGet("/user/login", (HttpContext http) =>
{
    var props = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
    {
        RedirectUri = "/" // where to go after successful login
    };
    return Results.Challenge(props, new[] { OpenIdConnectDefaults.AuthenticationScheme });
});

// 4) Logout (local cookie + OP sign-out)
app.MapGet("/user/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
        new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = "/"
        });
    return Results.Redirect("/");
});

// 5) Optional: whoami route for debugging
app.MapGet("/user/me", (HttpContext http) =>
{
    if (!(http.User.Identity?.IsAuthenticated ?? false))
        return Results.Json(new { authenticated = false });

    return Results.Json(new
    {
        authenticated = true,
        name = http.User.FindFirst(ClaimTypes.Name)?.Value ?? http.User.FindFirst("preferred_username")?.Value ?? "Unknown",
        sub = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        email = http.User.FindFirst(ClaimTypes.Email)?.Value
    });
});

app.Run();

static ICryptotrackerConfig GetConfig(WebApplicationBuilder builder)
{
    var root = Directory.GetCurrentDirectory();

    var ymlConfigPath = Path.Combine(root, "..", "config", "config.yml");
    var jsonConfigPath = Path.Combine(root, "..", "config", "config.json");

    if (builder.Environment.IsProduction())
    {
        ymlConfigPath = Path.Combine(root, "config", "config.yml");
        jsonConfigPath = Path.Combine(root, "config", "config.json");
    }

    if (File.Exists(ymlConfigPath))
    {
        var yml = File.ReadAllText(ymlConfigPath);

        var config = CryptotrackerConfig.LoadFromYml(yml);

        return config;
    }

    if (File.Exists(jsonConfigPath))
    {
        var json = File.ReadAllText(jsonConfigPath);

        var config = CryptotrackerConfig.LoadFromJson(json);

        return config;
    }

    throw new Exception("Config file not found");
}