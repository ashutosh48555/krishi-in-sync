using AgriSyncIndia.Models;
using AgriSyncIndia.Services;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AgriSyncDbContext context, IServiceProvider serviceProvider)
    {
        // Ensure database schema is created
        await context.Database.EnsureCreatedAsync();

        // 0. Seed Farm Clusters if empty
        if (!await context.FarmClusters.AnyAsync())
        {
            var initialClusters = new List<FarmCluster>
            {
                new FarmCluster
                {
                    Name = "IARI Pusa Smart Farm - New Delhi",
                    Region = "Delhi NCR",
                    Latitude = 28.6139,
                    Longitude = 77.2090,
                    TotalAcreage = 45.0,
                    PrimaryCrops = "Pusa Basmati 1121, Sharbati Wheat",
                    Description = "Premier national agro-meteorological research complex featuring smart greenhouse arrays.",
                    Status = "Active"
                },
                new FarmCluster
                {
                    Name = "PAU Ludhiana Smart Agro Block",
                    Region = "Punjab",
                    Latitude = 30.9010,
                    Longitude = 75.8573,
                    TotalAcreage = 120.0,
                    PrimaryCrops = "PBW-725 Wheat, PR-126 Paddy",
                    Description = "Extensive precision agriculture field testing center for northern Indo-Gangetic wheat-paddy cycle.",
                    Status = "Active"
                },
                new FarmCluster
                {
                    Name = "Karnal Agri-Tech Research Complex",
                    Region = "Haryana",
                    Latitude = 29.6857,
                    Longitude = 76.9905,
                    TotalAcreage = 60.0,
                    PrimaryCrops = "Basmati CSR-30, Mustard",
                    Description = "Soil salinity and micro-irrigation optimization testing ground.",
                    Status = "Active"
                },
                new FarmCluster
                {
                    Name = "Nagpur Citrus & Precision Cluster",
                    Region = "Maharashtra",
                    Latitude = 21.1458,
                    Longitude = 79.0882,
                    TotalAcreage = 35.0,
                    PrimaryCrops = "Nagpur Mandarin, Soybean",
                    Description = "Subtropical drip fertigation and canopy microclimate testbed.",
                    Status = "Active"
                }
            };
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
                CreatedAt = DateTime.UtcNow.AddHours(5.5).AddDays(-30)
            };
            var managerUser = new User
            {
                FullName = "Priya Verma",
                Email = "priya.verma@agrisync.in",
                PasswordHash = hasher.HashPassword(null!, "AgriSync@123"),
                Role = "Farm Manager",
                AffiliatedFarmCluster = "PAU Ludhiana Smart Agro Block",
                CreatedAt = DateTime.UtcNow.AddHours(5.5).AddDays(-20)
            };
            context.Users.AddRange(defaultUser, managerUser);
            await context.SaveChangesAsync();
        }

        if (await context.Zones.AnyAsync())
        {
            // Already seeded, sync fresh telemetry if latest log is older than 15 mins
            var latestLog = await context.SensorLogs.OrderByDescending(s => s.TimestampIST).FirstOrDefaultAsync();
            if (latestLog == null || latestLog.TimestampIST < DateTime.UtcNow.AddHours(5.5).AddMinutes(-15))
            {
                var existingWeatherService = serviceProvider.GetRequiredService<IOpenMeteoService>();
                await existingWeatherService.IngestAndSyncTelemetryAsync();
            }
            return;
        }

        // 1. Seed Discrete Microclimate Zones
        var zones = new List<Zone>
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

        context.Zones.AddRange(zones);
        await context.SaveChangesAsync();

        int polyhouseId = zones[0].ZoneId;
        int openFieldId = zones[1].ZoneId;
        int dripSectorId = zones[2].ZoneId;

        // 2. Seed Actuators
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
                LastTriggered = DateTime.UtcNow.AddHours(5.5).AddMinutes(-45)
            },
            new Actuator
            {
                Name = "Misting Pump High-Pressure",
                ZoneId = polyhouseId,
                IsAutoMode = true,
                CurrentStatus = "OFF",
                DeviceType = "Misting",
                PowerRatingKw = 1.5,
                LastTriggered = DateTime.UtcNow.AddHours(5.5).AddHours(-3)
            },
            new Actuator
            {
                Name = "Drip Solenoid Valves A/B",
                ZoneId = dripSectorId,
                IsAutoMode = true,
                CurrentStatus = "ON",
                DeviceType = "Irrigation",
                PowerRatingKw = 0.75,
                LastTriggered = DateTime.UtcNow.AddHours(5.5).AddMinutes(-12)
            }
        };

        context.Actuators.AddRange(actuators);

        // 3. Seed Automation Rules
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

        // 4. Seed Users
        var users = new List<User>
        {
            new User
            {
                FullName = "Dr. Rajesh Sharma",
                Email = "dr.sharma@agrisync.in",
                PasswordHash = "AQAAAAEAACcQAAAAEFakeHashForDemoAuth1234567890=",
                Role = "Agronomist",
                CreatedAt = DateTime.UtcNow.AddHours(5.5).AddDays(-30)
            },
            new User
            {
                FullName = "Priya Verma",
                Email = "priya.verma@agrisync.in",
                PasswordHash = "AQAAAAEAACcQAAAAEFakeHashForDemoAuth0987654321=",
                Role = "Farm Manager",
                CreatedAt = DateTime.UtcNow.AddHours(5.5).AddDays(-20)
            }
        };

        context.Users.AddRange(users);

        // 5. Seed 24-Hour Historical Sensor Logs for all 3 Zones
        var nowIst = DateTime.UtcNow.AddHours(5.5);
        var logs = new List<SensorLog>();
        var weatherService = serviceProvider.GetRequiredService<IOpenMeteoService>();

        for (int i = 24; i >= 0; i--)
        {
            var logTime = nowIst.AddHours(-i);
            int hour = logTime.Hour;

            // Diurnal temperature wave (sinusoidal pattern matching northern Indian plains)
            double sunFactor = Math.Sin((hour - 8) * Math.PI / 12.0);
            double baseAmbientTemp = 24.0 + (sunFactor * 6.5); // 17.5°C to 30.5°C
            double baseAmbientRh = 72.0 - (sunFactor * 24.0);  // 48% to 96%
            double baseSoilMoist = 36.0 + Math.Sin(hour * 0.2) * 2.5;

            // Zone 1: Polyhouse (+3.2°C, +8.5% RH)
            double pTemp = Math.Round(baseAmbientTemp + 3.2, 1);
            double pRh = Math.Round(Math.Min(95.0, baseAmbientRh + 8.5), 1);
            double pVpd = weatherService.CalculateVPD(pTemp, pRh);
            logs.Add(new SensorLog
            {
                ZoneId = polyhouseId,
                Temperature = pTemp,
                Humidity = pRh,
                VPD = pVpd,
                SoilMoisture = Math.Round(baseSoilMoist + 12.0, 1),
                SolarRadiation = hour is >= 6 and <= 18 ? Math.Round(Math.Max(50.0, 520.0 * Math.Sin((hour - 6) * Math.PI / 12.0)), 0) : 0,
                SoilTemperature = Math.Round(pTemp - 1.5, 1),
                WindSpeed = 0.4,
                TimestampIST = logTime
            });

            // Zone 2: Open Field (Ambient baseline)
            double oTemp = Math.Round(baseAmbientTemp, 1);
            double oRh = Math.Round(baseAmbientRh, 1);
            double oVpd = weatherService.CalculateVPD(oTemp, oRh);
            logs.Add(new SensorLog
            {
                ZoneId = openFieldId,
                Temperature = oTemp,
                Humidity = oRh,
                VPD = oVpd,
                SoilMoisture = Math.Round(baseSoilMoist, 1),
                SolarRadiation = hour is >= 6 and <= 18 ? Math.Round(Math.Max(80.0, 780.0 * Math.Sin((hour - 6) * Math.PI / 12.0)), 0) : 0,
                SoilTemperature = Math.Round(oTemp - 0.8, 1),
                WindSpeed = Math.Round(4.2 + (sunFactor * 3.5), 1),
                TimestampIST = logTime
            });

            // Zone 3: Drip Sector (-1.2°C, +4.0% RH)
            double dTemp = Math.Round(baseAmbientTemp - 1.2, 1);
            double dRh = Math.Round(Math.Min(92.0, baseAmbientRh + 4.0), 1);
            double dVpd = weatherService.CalculateVPD(dTemp, dRh);
            logs.Add(new SensorLog
            {
                ZoneId = dripSectorId,
                Temperature = dTemp,
                Humidity = dRh,
                VPD = dVpd,
                SoilMoisture = Math.Round(baseSoilMoist + 24.0, 1),
                SolarRadiation = hour is >= 6 and <= 18 ? Math.Round(Math.Max(70.0, 690.0 * Math.Sin((hour - 6) * Math.PI / 12.0)), 0) : 0,
                SoilTemperature = Math.Round(dTemp - 2.2, 1),
                WindSpeed = Math.Round(3.5 + (sunFactor * 2.0), 1),
                TimestampIST = logTime
            });
        }

        context.SensorLogs.AddRange(logs);

        // 6. Seed Climate Alerts & Advisories
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
}
