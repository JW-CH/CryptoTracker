using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using System.Text.Json;
using System.Text.Json.Serialization;
using cryptotracker.core.Interfaces;
using cryptotracker.core.Logic.CryptoPriceProviders;
using cryptotracker.core.Logic.Integrations;
using cryptotracker.core.Logic.StockPriceProviders;
using cryptotracker.core.Logic.CurrencyPriceProviders;
using cryptotracker.core.Models;
using cryptotracker.database.Models;
using cryptotracker.webapi.Backgroundservices;
using cryptotracker.webapi.Configuration;
using cryptotracker.webapi.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// config sources (later sources override earlier ones):
// config.yml (or config.json) from CONFIG_PATH, then CRYPTOTRACKER_* env vars
// (nested keys via double underscore, e.g. CRYPTOTRACKER_AUTH__SECRET)
var configDir = Environment.GetEnvironmentVariable("CONFIG_PATH")
    ?? Path.Combine(Directory.GetCurrentDirectory(), builder.Environment.IsProduction() ? "config" : Path.Combine("..", "config"));

var ymlConfigPath = Path.Combine(configDir, "config.yml");
var jsonConfigPath = Path.Combine(configDir, "config.json");

if (File.Exists(ymlConfigPath))
{
    builder.Configuration.AddYamlFile(ymlConfigPath);
}
else if (File.Exists(jsonConfigPath))
{
    builder.Configuration.AddJsonFile(jsonConfigPath);
}

builder.Configuration.AddEnvironmentVariables("CRYPTOTRACKER_");

var config = builder.Configuration.Get<CryptoTrackerConfig>() ?? new CryptoTrackerConfig();

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
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICryptoTrackerConfig>(srv =>
{
    return config;
});

builder.Services.AddSingleton(TimeProvider.System);
// constructed eagerly so an invalid timezone fails at startup, not on first request
builder.Services.AddSingleton(new PortfolioClock(TimeProvider.System, config));

builder.Services.AddSingleton<IEnumerable<IIntegrationProvider>>(srv =>
{
    var httpClientFactory = srv.GetRequiredService<IHttpClientFactory>();

    return new List<IIntegrationProvider>
    {
        new BitpandaIntegrationProvider(httpClientFactory),
        new CoinbaseIntegrationProvider(),
        new BinanceIntegrationProvider(),
        new KucoinIntegrationProvider(),
        new CryptocomIntegrationProvider(),
        new BitcoinIntegrationProvider(httpClientFactory),
        new EthereumIntegrationProvider(httpClientFactory),
        new RippleIntegrationProvider(httpClientFactory),
    };
});

builder.Services.AddSingleton<IEnumerable<IPriceProvider>>(srv =>
{
    var list = new List<IPriceProvider>();

    var config = srv.GetRequiredService<ICryptoTrackerConfig>();
    var httpClientFactory = srv.GetRequiredService<IHttpClientFactory>();
    var memoryCache = srv.GetRequiredService<IMemoryCache>();

    var currencyPriceProvider = new FrankfurterCurrencyPriceProvider(srv.GetRequiredService<ILogger<FrankfurterCurrencyPriceProvider>>(), httpClientFactory, memoryCache);
    list.Add(currencyPriceProvider);
    list.Add(new CoingeckoPriceProvider(httpClientFactory, memoryCache));

    if (config.StockApi.HasValue)
    {
        switch (config.StockApi)
        {
            case StockApi.YahooFinance:
                list.Add(new YahooFinancePriceProvider(srv.GetRequiredService<ILogger<YahooFinancePriceProvider>>(), currencyPriceProvider));
                break;
            default:
                throw new Exception($"Unknown StockApi: {config.StockApi}");
        }
    }

    return list;
});

builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<PortfolioQueryService>();
builder.Services.AddScoped<AssetMetadataService>();
builder.Services.AddScoped<AssetService>();
builder.Services.AddScoped<IntegrationService>();
builder.Services.AddScoped<MeasuringService>();

// DbContext
builder.Services.AddDbContext<DatabaseContext>((serviceProvider, options) =>
{
    var config = serviceProvider.GetRequiredService<ICryptoTrackerConfig>();
    var connectionString = config?.ConnectionString ?? "";
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("ConnectionString is missing. Configure CryptoTracker:ConnectionString.");
    }
    options.UseNpgsql(connectionString).LogTo(Console.WriteLine, LogLevel.Warning);
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
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
// JWT-Validation
.AddJwtBearer(jwtOptions =>
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
});

// OpenID Connect – only when configured
if (config.Oidc.IsEnabled)
{
    authBuilder
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

            oidcOptions.Events = new OpenIdConnectEvents
            {
                OnTokenValidated = async ctx =>
                {
                    var userManager = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                    var email = ctx.Principal?.FindFirstValue(ClaimTypes.Email) ?? ctx.Principal?.FindFirst("email")?.Value ?? "";

                    if (!string.IsNullOrEmpty(email))
                    {
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
                    else
                    {
                        ctx.Fail("Email claim not found");
                        return;
                    }
                }
            };
        });
}

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// brute-force protection for credential endpoints (login/register).
// note: behind a reverse proxy without X-Forwarded-For handling all clients
// share the proxy ip, which makes the limit stricter, not weaker
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

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

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
