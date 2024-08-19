using Microsoft.EntityFrameworkCore;

using Palace.Server.Configuration;

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
        modelBuilder.Entity<MicroServiceSettings>()
            .ToTable("MicroServiceSetting")
            .HasKey(i => i.Id);

        modelBuilder.Entity<ArgumentsByHost>()
            .ToTable("ArgumentsByHost")
            .HasKey(i => i.Id);
    }

    public DbSet<MicroServiceSettings> MicroServiceSettings { get; set; } = default!;
    public DbSet<ArgumentsByHost> ArgumentsByHosts { get; set; } = default!;
}
