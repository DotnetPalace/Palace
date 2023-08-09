using Palace.Server.Configuration;
using Palace.Shared;

using Microsoft.EntityFrameworkCore;

namespace Palace.Server.Services;

public class PalaceDbContext : DbContext
{
    private readonly SqliteSettings _settings;

    public PalaceDbContext(SqliteSettings settings)
    {
        _settings = settings;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        optionsBuilder.UseSqlite(_settings.ConnectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var microServiceSettings = modelBuilder.Entity<MicroServiceSettings>()
            .ToTable("MicroServiceSetting")
            .HasKey(i => i.Id);
    }

    public DbSet<MicroServiceSettings> MicroServiceSettings { get; set; } = default!;

}
