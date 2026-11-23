using AgriSyncIndia.Models;
using AgriSyncIndia.Services;

namespace AgriSyncIndia.Data;

/// <summary>Static seed content for DbInitializer.</summary>
internal static class SeedData
{
    private const int SeedHours = 7 * 24;

    private static double SeedVpd(double tempC, double rh) => Math.Round(ClimateRules.Vpd(tempC, rh), 2);

    public static List<FarmCluster> BuildClusters()
    {
        return new List<FarmCluster>
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
    }

    public static List<SensorLog> BuildLogs(DateTime nowIst, int polyhouseId, int openFieldId, int dripSectorId)
    {
        var logs = new List<SensorLog>();
        for (int i = SeedHours - 1; i >= 0; i--)
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
            double pVpd = SeedVpd(pTemp, pRh);
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
            double oVpd = SeedVpd(oTemp, oRh);
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
            double dVpd = SeedVpd(dTemp, dRh);
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
        return logs;
    }
}
