using AgriSyncIndia.Services;
using AgriSyncIndia.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Data;

public class AgriSyncDbContext : DbContext, IDataProtectionKeyContext
{
    public AgriSyncDbContext(DbContextOptions<AgriSyncDbContext> options) : base(options)
    {
    }

    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<SensorLog> SensorLogs => Set<SensorLog>();
    public DbSet<Actuator> Actuators => Set<Actuator>();
    public DbSet<AutomationRule> AutomationRules => Set<AutomationRule>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ClimateAlert> ClimateAlerts => Set<ClimateAlert>();
    public DbSet<FarmCluster> FarmClusters => Set<FarmCluster>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Zone entity mapping
        modelBuilder.Entity<Zone>(entity =>
        {
            entity.HasKey(z => z.ZoneId);
            entity.Property(z => z.Name).IsRequired().HasMaxLength(100);
            entity.Property(z => z.CropType).IsRequired().HasMaxLength(100);
            entity.Property(z => z.Status).HasMaxLength(50);
        });

        // SensorLog entity mapping
        modelBuilder.Entity<SensorLog>(entity =>
        {
            entity.HasKey(s => s.LogId);
            entity.HasIndex(s => s.TimestampIST);
            entity.HasIndex(s => new { s.ZoneId, s.TimestampIST });

            entity.HasOne(s => s.Zone)
                .WithMany(z => z.SensorLogs)
                .HasForeignKey(s => s.ZoneId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Actuator entity mapping
        modelBuilder.Entity<Actuator>(entity =>
        {
            entity.HasKey(a => a.ActuatorId);
            entity.Property(a => a.Name).IsRequired().HasMaxLength(100);
            entity.Property(a => a.CurrentStatus).HasMaxLength(20);

            entity.HasOne(a => a.Zone)
                .WithMany(z => z.Actuators)
                .HasForeignKey(a => a.ZoneId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AutomationRule entity mapping
        modelBuilder.Entity<AutomationRule>(entity =>
        {
            entity.HasKey(r => r.RuleId);
            entity.Property(r => r.ConditionVariable).IsRequired().HasMaxLength(50);
            entity.Property(r => r.Operator).IsRequired().HasMaxLength(10);
            entity.Property(r => r.ActionTarget).IsRequired().HasMaxLength(100);
        });

        // User entity mapping
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.ClerkUserId).IsUnique();
        });

        // ClimateAlert entity mapping
        modelBuilder.Entity<ClimateAlert>(entity =>
        {
            entity.HasKey(a => a.AlertId);
            entity.Property(a => a.Severity).IsRequired().HasMaxLength(20);
            entity.Property(a => a.Title).IsRequired().HasMaxLength(150);

            entity.HasOne(a => a.Zone)
                .WithMany(z => z.Alerts)
                .HasForeignKey(a => a.ZoneId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    /// <summary>
    /// Fetches the last 24 hours of sensor logs for a specific zone.
    /// Supports direct execution matching the C# service / SQL Stored Procedure pattern.
    /// </summary>
    public async Task<List<SensorLog>> Get24HourZoneLogsAsync(int zoneId)
    {
        var cutoff = IstClock.Now.AddHours(-24);
        return await SensorLogs
            .Where(s => s.ZoneId == zoneId && s.TimestampIST >= cutoff)
            .OrderBy(s => s.TimestampIST)
            .AsNoTracking()
            .ToListAsync();
    }
}
