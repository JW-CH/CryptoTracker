using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<ExchangeIntegration> ExchangeIntegrations { get; set; }
    public DbSet<Asset> Assets { get; set; }
    public DbSet<AssetMeasuring> AssetMeasurings { get; set; }
    public DbSet<AssetPriceHistory> AssetPriceHistory { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 10);
    }
}