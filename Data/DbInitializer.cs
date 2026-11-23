using AgriSyncIndia.Models;
using AgriSyncIndia.Services;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Data;

public static class DbInitializer
{
        /// <summary>
    /// Idempotent seed: every table is filled only when empty. Schema must already exist
    /// (Migrate on Postgres, EnsureCreated on SQLite). Logs and rethrows on failure.
    /// </summary>
    public static async Task InitializeAsync(AgriSyncDbContext context, IServiceProvider serviceProvider)
    {
        try
        {
            await SeedAsync(context, serviceProvider);
        }
        catch (Exception ex)
        {
            serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("DbInitializer")
                .LogError(ex, "Seeding the AgriSync database failed.");
            throw;
        }
    }

    private static async Task SeedAsync(AgriSyncDbContext context, IServiceProvider serviceProvider)
    {
        // 0. Seed Farm Clusters if empty
        if (!await context.FarmClusters.AnyAsync())
        {
            var initialClusters = SeedData.BuildClusters();
            context.FarmClusters.AddRange(initialClusters);
            await context.SaveChangesAsync();
        }

        // Ensure default users have verifiable password hashes
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        if (!await context.Users.AnyAsync())
        {
            var defaultUser = new User
            {
                FullName = "Dr. Rajesh Sharma",
                Email = "dr.sharma@agrisync.in",
                PasswordHash = hasher.HashPassword(null!, "AgriSync@123"),
                Role = "Agronomist",
                AffiliatedFarmCluster = "IARI Pusa Smart Farm - New Delhi",
                CreatedAt = IstClock.Now.AddDays(-30)
            };
            var managerUser = new User
            {
                FullName = "Priya Verma",
                Email = "priya.verma@agrisync.in",
                PasswordHash = hasher.HashPassword(null!, "AgriSync@123"),
                Role = "Farm Manager",
                AffiliatedFarmCluster = "PAU Ludhiana Smart Agro Block",
                CreatedAt = IstClock.Now.AddDays(-20)
            };
            context.Users.AddRange(defaultUser, managerUser);
            await context.SaveChangesAsync();
        }

        // 1. Seed Discrete Microclimate Zones
        if (!await context.Zones.AnyAsync())
        {
            var seedZones = new List<Zone>
            {
                new Zone
                {
                    Name = "Polyhouse Nursery",
                    CropType = "Pusa Basmati 1121",
                    TargetTemp = 24.5,
                    TargetHumidity = 75.0,
                    Status = "Optimal",
                    Description = "High-tech polyethylene nursery protecting Basmati paddy seedlings against extreme diurnal shifts.",
                    AreaSqMeters = 800,
                    IconClass = "bi-flower1"
                },
                new Zone
                {
                    Name = "Open Field",
                    CropType = "Sharbati Premium Wheat",
                    TargetTemp = 26.0,
                    TargetHumidity = 60.0,
                    Status = "Optimal",
                    Description = "Unsheltered open-field acreage monitored for real-time evapotranspiration and solar radiation.",
                    AreaSqMeters = 5000,
                    IconClass = "bi-tree"
                },
                new Zone
                {
                    Name = "Drip Sector",
                    CropType = "Kufri Jyoti Potato",
                    TargetTemp = 22.0,
                    TargetHumidity = 65.0,
                    Status = "Optimal",
                    Description = "Precision fertigation block with buried micro-drip laterals and root-zone capacitance sensors.",
                    AreaSqMeters = 2400,
                    IconClass = "bi-droplet-half"
                }
            };

            context.Zones.AddRange(seedZones);
            await context.SaveChangesAsync();
        }

        var zones = await context.Zones.OrderBy(z => z.ZoneId).ToListAsync();
        int polyhouseId = zones.First(z => z.Name == "Polyhouse Nursery").ZoneId;
        int openFieldId = zones.First(z => z.Name == "Open Field").ZoneId;
        int dripSectorId = zones.First(z => z.Name == "Drip Sector").ZoneId;

        // 2. Seed Actuators
        if (!await context.Actuators.AnyAsync())
        {
            var actuators = new List<Actuator>
            {
                new Actuator
                {
                    Name = "Exhaust Fans 1 & 2",
                    ZoneId = polyhouseId,
                    IsAutoMode = true,
                    CurrentStatus = "OFF",
                    DeviceType = "Ventilation",
                    PowerRatingKw = 2.2,
                    LastTriggered = IstClock.Now.AddMinutes(-45)
                },
                new Actuator
                {
                    Name = "Misting Pump High-Pressure",
                    ZoneId = polyhouseId,
                    IsAutoMode = true,
                    CurrentStatus = "OFF",
                    DeviceType = "Misting",
                    PowerRatingKw = 1.5,
                    LastTriggered = IstClock.Now.AddHours(-3)
                },
                new Actuator
                {
                    Name = "Drip Solenoid Valves A/B",
                    ZoneId = dripSectorId,
                    IsAutoMode = true,
                    CurrentStatus = "ON",
                    DeviceType = "Irrigation",
                    PowerRatingKw = 0.75,
                    LastTriggered = IstClock.Now.AddMinutes(-12)
                }
            };

            context.Actuators.AddRange(actuators);
            await context.SaveChangesAsync();
        }
        // 3. Seed Automation Rules
        if (!await context.AutomationRules.AnyAsync())
        {
            var rules = new List<AutomationRule>
            {
                new AutomationRule
                {
                    ConditionVariable = "Relative Humidity",
                    Operator = ">",
                    ThresholdValue = 75.0,
                    ActionTarget = "Turn ON Exhaust Fans",
                    IsActive = true,
                    Description = "Ventilate polyhouse when air moisture exceeds 75% to prevent rice blast / downy mildew spores."
                },
                new AutomationRule
                {
                    ConditionVariable = "Temperature",
                    Operator = ">",
                    ThresholdValue = 34.0,
                    ActionTarget = "Turn ON Misting Pump",
                    IsActive = true,
                    Description = "Activate fine droplet evaporative misting when ambient polyhouse temp crosses 34°C."
                },
                new AutomationRule
                {
                    ConditionVariable = "Soil Moisture",
                    Operator = "<",
                    ThresholdValue = 35.0,
                    ActionTarget = "Turn ON Drip Solenoid Valves",
                    IsActive = true,
                    Description = "Initiate drip irrigation pulse when root-zone volumetric soil moisture drops below 35%."
                },
                new AutomationRule
                {
                    ConditionVariable = "VPD",
                    Operator = "<",
                    ThresholdValue = 0.45,
                    ActionTarget = "Turn ON Exhaust Fans",
                    IsActive = true,
                    Description = "Expel saturated boundary air when VPD drops below 0.45 kPa to restore nutrient transpiration."
                }
            };

            context.AutomationRules.AddRange(rules);
            await context.SaveChangesAsync();
        }
        // 5. Seed 7 days of hourly Sensor Logs for all 3 Zones (504 rows)
        if (!await context.SensorLogs.AnyAsync())
        {
            var nowIst = IstClock.Now;
            var logs = new List<SensorLog>();

            logs.AddRange(SeedData.BuildLogs(nowIst, polyhouseId, openFieldId, dripSectorId));

            context.SensorLogs.AddRange(logs);
            await context.SaveChangesAsync();
        }

        // 6. Seed Climate Alerts & Advisories
        if (!await context.ClimateAlerts.AnyAsync())
        {
            var nowIst = IstClock.Now;
            var alerts = new List<ClimateAlert>
            {
                new ClimateAlert
                {
                    ZoneId = polyhouseId,
                    Severity = "Critical",
                    Title = "High Humidity Fungal Risk Warning",
                    Message = "Relative humidity exceeded 80% during pre-dawn hours (04:00 - 06:00 IST). Microclimate conditions are severely favorable for Rice Blast (Magnaporthe oryzae) spore germination.",
                    AgronomistRecommendation = "Activate exhaust ventilation fans immediately. Ensure adequate air circulation and withhold nitrogenous top-dressing until canopy dries.",
                    TimestampIST = nowIst.AddHours(-2),
                    IsAcknowledged = false
                },
                new ClimateAlert
                {
                    ZoneId = openFieldId,
                    Severity = "Warning",
                    Title = "Heatwave & Dry Boundary Layer Advisory",
                    Message = "Afternoon peak temperature forecast to touch 31.8°C with VPD exceeding 1.45 kPa in Open Field wheat block.",
                    AgronomistRecommendation = "Schedule light sprinkler cooling or evening furrow irrigation to mitigate terminal heat stress.",
                    TimestampIST = nowIst.AddHours(-5),
                    IsAcknowledged = false
                },
                new ClimateAlert
                {
                    ZoneId = dripSectorId,
                    Severity = "Info",
                    Title = "Drip Irrigation Pulse Completed",
                    Message = "Automated fertigation solenoid completed a 45-minute cycle. Root-zone volumetric soil moisture restored to 58.4%.",
                    AgronomistRecommendation = "Maintain current cycle interval. Check emitter flow meters for zero calcification.",
                    TimestampIST = nowIst.AddHours(-1),
                    IsAcknowledged = true
                }
            };

            context.ClimateAlerts.AddRange(alerts);
            await context.SaveChangesAsync();
        }

        // Top up telemetry when the newest log is stale (best effort, needs the weather service).
        var latestLog = await context.SensorLogs.OrderByDescending(s => s.TimestampIST).FirstOrDefaultAsync();
        var weatherService = serviceProvider.GetService<IOpenMeteoService>();
        if (weatherService != null && latestLog != null && latestLog.TimestampIST < IstClock.Now.AddMinutes(-15))
        {
            await weatherService.IngestAndSyncTelemetryAsync();
        }
    }
}
