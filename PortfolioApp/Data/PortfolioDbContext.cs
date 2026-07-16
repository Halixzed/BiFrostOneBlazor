using Microsoft.EntityFrameworkCore;
using PortfolioApp.Models;

namespace PortfolioApp.Data;

public class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : DbContext(options)
{
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Watermark> Watermarks => Set<Watermark>();
    public DbSet<EnvironmentMap> EnvironmentMaps => Set<EnvironmentMap>();
    public DbSet<Device> Devices => Set<Device>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Unit>(entity =>
        {
            entity.OwnsOne(u => u.Rotation);
        });
    }
}
