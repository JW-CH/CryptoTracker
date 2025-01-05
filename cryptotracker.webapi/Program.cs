using cryptotracker.core.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddSimpleConsole(options =>
                {
                    // Customizing the log output format
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";  // Custom timestamp format
                })
            .SetMinimumLevel(LogLevel.Information); // Set minimum log level

// Add services to the container.

builder.Services.AddSingleton<CryptotrackerConfig>(srv =>
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
});

builder.Services.AddDbContext<DatabaseContext>((serviceProvider, options) =>
{
    var config = serviceProvider.GetRequiredService<CryptotrackerConfig>();
    var connectionString = config?.ConnectionString ?? "";
    options.UseMySQL(connectionString).LogTo(Console.WriteLine, LogLevel.Warning);
    options.EnableSensitiveDataLogging(false);
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Disable EF Core info logs
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

var app = builder.Build();

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

app.Run();
