using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;

public class ApplicationContext(DbContextOptions<ApplicationContext> options) : DbContext(options)
{
    public DbSet<ExchangeIntegration> ExchangeIntegrations { get; set; }
    public DbSet<Asset> Assets { get; set; }
    public DbSet<AssetMeasuring> AssetMeasurings { get; set; }
}