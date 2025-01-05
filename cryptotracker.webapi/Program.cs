using cryptotracker.core.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

// Add services to the container.

builder.Services.AddSingleton<CryptotrackerConfig>(srv =>
{
    var root = Directory.GetCurrentDirectory();

    var ymlConfigPath = Path.Combine(root, "..", "config", "config.yml");

    if (File.Exists(ymlConfigPath))
    {
        var yml = File.ReadAllText(ymlConfigPath);

        var config = CryptotrackerConfig.LoadFromYml(yml);

        return config;
    }

    var jsonConfigPath = Path.Combine(root, "..", "config", "config.json");

    if (File.Exists(jsonConfigPath))
    {
        var json = File.ReadAllText(jsonConfigPath);

        var config = CryptotrackerConfig.LoadFromJson(json);

        return config;
    }

    return new CryptotrackerConfig();
});

builder.Services.AddDbContext<DatabaseContext>((serviceProvider, options) =>
{
    var config = serviceProvider.GetRequiredService<CryptotrackerConfig>();
    var connectionString = config?.ConnectionString ?? "";
    options.UseMySQL(connectionString);
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
