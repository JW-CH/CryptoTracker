using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using cryptotracker.core.Interfaces;
using cryptotracker.core.Logic;
using cryptotracker.core.Models;
using cryptotracker.database.Models;
using cryptotracker.webapi.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

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

builder.Services.AddSingleton<ICryptoTrackerConfig>(srv =>
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
    ILogger logger;
    var fiatLogic = srv.GetRequiredService<IFiatLogic>();
    var config = srv.GetRequiredService<ICryptoTrackerConfig>();

    if (string.IsNullOrWhiteSpace(config?.StockApi))
    {
        logger = srv.GetRequiredService<ILogger<EmptyStockLogic>>();
        return new EmptyStockLogic(logger);
    }

    logger = srv.GetRequiredService<ILogger<YahooFinanceStockLogic>>();
    return new YahooFinanceStockLogic(logger, fiatLogic);
});

builder.Services.AddSingleton<JwtService>();

// DbContext
builder.Services.AddDbContext<DatabaseContext>((serviceProvider, options) =>
{
    var config = serviceProvider.GetRequiredService<ICryptoTrackerConfig>();
    var connectionString = config?.ConnectionString ?? "";
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("ConnectionString is missing. Configure CryptoTracker:ConnectionString.");
    }
    options.UseMySQL(connectionString).LogTo(Console.WriteLine, LogLevel.Warning);
    options.EnableSensitiveDataLogging(false);
});

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<DatabaseContext>()
    .AddDefaultTokenProviders();

// JWT Auth
var secretKey = Encoding.UTF8.GetBytes(config.Auth.Secret ?? throw new Exception("JWT Secret not configured"));
if (secretKey.Length < 32)
{
    throw new Exception("JWT Secret must be at least 32 bytes (256 bits) for HMAC SHA256");
}

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
// Required for OIDC sign-in (OpenIdConnect needs a sign-in handler)
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
// JWT-Validation
.AddJwtBearer(jwtOptions =>
{
    jwtOptions.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = config.Auth.Issuer,
        ValidAudience = config.Auth.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
    };

    jwtOptions.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Prefer Bearer header; fall back to cookie
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                var cookie = context.Request.Cookies["jwt"];
                if (!string.IsNullOrEmpty(cookie))
                {
                    context.Token = cookie;
                }
            }
            return Task.CompletedTask;
        }
    };
})
// OpenID Connect (OIDC Provider, z. B. PocketID)
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

    oidcOptions.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = async ctx =>
        {
            var userManager = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var email = ctx.Principal.FindFirstValue(ClaimTypes.Email) ?? ctx.Principal.FindFirst("email")?.Value;

            if (!string.IsNullOrEmpty(email))
            {
                var jwtService = ctx.HttpContext.RequestServices.GetRequiredService<JwtService>();
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new ApplicationUser { Email = email, UserName = email, EmailConfirmed = true };
                    var createResult = await userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        ctx.Fail("User creation failed");
                        return;
                    }
                }
                var jwt = jwtService.GenerateJwtToken(user);
                jwtService.SetJwtCookie(ctx.Response, jwt);
            }
            else
            {
                ctx.Fail("Email claim not found");
                return;
            }
        }
    };
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<UpdateService>();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto
});

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();

static CryptoTrackerConfig GetConfig(WebApplicationBuilder builder)
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

        var config = CryptoTrackerConfig.LoadFromYml(yml);

        return config;
    }

    if (File.Exists(jsonConfigPath))
    {
        var json = File.ReadAllText(jsonConfigPath);

        var config = CryptoTrackerConfig.LoadFromJson(json);

        return config;
    }

    throw new Exception("Config file not found");
}