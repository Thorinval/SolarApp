using Microsoft.EntityFrameworkCore;
using SolarApp.Models;

namespace SolarApp.Data;

public class AtmocDbContext : DbContext
{
    public AtmocDbContext(DbContextOptions<AtmocDbContext> options) : base(options)
    {
    }

    public DbSet<Site> Sites { get; set; }
    public DbSet<SiteDataSnapshot> SiteDataSnapshots { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<DeviceDataSnapshot> DeviceDataSnapshots { get; set; }
    public DbSet<DeviceAlert> DeviceAlerts { get; set; }
    public DbSet<DailyEnergyRecord> DailyEnergyRecords { get; set; }
    public DbSet<ApiToken> ApiTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuration pour Site
        modelBuilder.Entity<Site>()
            .HasKey(s => s.Id);

        modelBuilder.Entity<Site>()
            .HasIndex(s => s.SiteId)
            .IsUnique();

        modelBuilder.Entity<Site>()
            .HasMany(s => s.DataSnapshots)
            .WithOne(ds => ds.Site)
            .HasForeignKey(ds => ds.SiteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Site>()
            .HasMany(s => s.Devices)
            .WithOne(d => d.Site)
            .HasForeignKey(d => d.SiteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configuration pour SiteDataSnapshot
        modelBuilder.Entity<SiteDataSnapshot>()
            .HasKey(s => s.Id);

        modelBuilder.Entity<SiteDataSnapshot>()
            .HasIndex(s => new { s.SiteId, s.SnapshotTime });

        // Configuration pour Device
        modelBuilder.Entity<Device>()
            .HasKey(d => d.Id);

        modelBuilder.Entity<Device>()
            .HasIndex(d => d.DeviceSerialNumber)
            .IsUnique();

        modelBuilder.Entity<Device>()
            .HasMany(d => d.DataSnapshots)
            .WithOne(ds => ds.Device)
            .HasForeignKey(ds => ds.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configuration pour DeviceDataSnapshot
        modelBuilder.Entity<DeviceDataSnapshot>()
            .HasKey(d => d.Id);

        modelBuilder.Entity<DeviceDataSnapshot>()
            .HasIndex(d => new { d.DeviceId, d.SnapshotTime });

        // Configuration pour DeviceAlert
        modelBuilder.Entity<DeviceAlert>()
            .HasKey(da => da.Id);

        modelBuilder.Entity<DeviceAlert>()
            .HasIndex(da => new { da.SiteId, da.AlarmOccurTime });

        modelBuilder.Entity<DeviceAlert>()
            .HasIndex(da => da.AlarmLevel);

        // Configuration pour DailyEnergyRecord
        modelBuilder.Entity<DailyEnergyRecord>()
            .HasKey(der => der.Id);

        modelBuilder.Entity<DailyEnergyRecord>()
            .HasIndex(der => der.RecordDate)
            .IsUnique();

        // Configuration pour ApiToken
        modelBuilder.Entity<ApiToken>()
            .HasKey(at => at.Id);
    }
}

