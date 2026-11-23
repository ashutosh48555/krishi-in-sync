using System.Globalization;
using System.Text;
using AgriSyncIndia.Models;
using AgriSyncIndia.Services;
using AgriSyncIndia.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Controllers;

public partial class ClimateController
{
    /// <summary>
    /// View 2: Microclimate Topology (/Climate/Topology)
    /// </summary>
    [HttpGet("Topology")]
    public async Task<IActionResult> Topology()
    {
        var zones = await _context.Zones.ToListAsync();
        var recentLogs = await _context.SensorLogs
            .OrderByDescending(s => s.TimestampIST)
            .Take(zones.Count * 2)
            .ToListAsync();

        var openLog = recentLogs.FirstOrDefault(l => l.ZoneId == 2) ?? new SensorLog { Temperature = 24.5, Humidity = 65.0 };
        double ambientBaseline = openLog.Temperature;

        var sectors = new List<ThermalSectorInfo>();
        foreach (var z in zones)
        {
            var log = recentLogs.FirstOrDefault(l => l.ZoneId == z.ZoneId) ?? new SensorLog
            {
                Temperature = z.TargetTemp,
                Humidity = z.TargetHumidity,
                VPD = 0.9,
                SoilMoisture = 55.0
            };

            double delta = Math.Round(log.Temperature - ambientBaseline, 1);
            string thermalCategory = delta > 1.5 ? "Thermal Gain (Greenhouse)" : (delta < -0.8 ? "Evaporative Cooling" : "Ambient Baseline");
            string colorBadge = delta > 1.5 ? "border-danger text-danger" : (delta < -0.8 ? "border-primary text-primary" : "border-success text-success");

            sectors.Add(new ThermalSectorInfo
            {
                SectorCode = $"SEC-0{z.ZoneId}",
                ZoneName = z.Name,
                MicroclimateType = z.Name.Contains("Polyhouse") ? "Protected Controlled Cladding" : (z.Name.Contains("Drip") ? "Precision Drip Lateral Canopy" : "Open Agro-Meteorological"),
                CurrentTemp = log.Temperature,
                AmbientDelta = delta,
                Humidity = log.Humidity,
                VPD = log.VPD,
                SoilMoisture = log.SoilMoisture,
                ThermalCategory = thermalCategory,
                ColorBadge = colorBadge
            });
        }

        var nowIst = IstClock.Now;
        var nodes = new List<SensorNodeHealth>
        {
            new SensorNodeHealth
            {
                NodeId = "NODE-IN-01",
                ZoneName = "Polyhouse Nursery",
                SensorType = "SHT40 (Temp/RH) + Light Pyranometer",
                BatteryPercent = 94,
                SignalRssi = -58,
                TransmissionInterval = "5 min",
                LastHeartbeatIST = nowIst.AddMinutes(-3),
                LastCalibrationDate = nowIst.AddDays(-15),
                HealthStatus = "Healthy"
            },
            new SensorNodeHealth
            {
                NodeId = "NODE-IN-02",
                ZoneName = "Polyhouse Canopy Top",
                SensorType = "Infrared Canopy Thermal Sensor",
                BatteryPercent = 88,
                SignalRssi = -64,
                TransmissionInterval = "10 min",
                LastHeartbeatIST = nowIst.AddMinutes(-7),
                LastCalibrationDate = nowIst.AddDays(-28),
                HealthStatus = "Healthy"
            },
            new SensorNodeHealth
            {
                NodeId = "NODE-IN-03",
                ZoneName = "Open Field Mast",
                SensorType = "Ultrasonic Anemometer & Solar Pyranometer",
                BatteryPercent = 76,
                SignalRssi = -71,
                TransmissionInterval = "15 min",
                LastHeartbeatIST = nowIst.AddMinutes(-4),
                LastCalibrationDate = nowIst.AddDays(-45),
                HealthStatus = "Healthy"
            },
            new SensorNodeHealth
            {
                NodeId = "NODE-IN-04",
                ZoneName = "Drip Sector Sub-surface",
                SensorType = "FDR Multi-depth Soil Moisture & EC Probe",
                BatteryPercent = 91,
                SignalRssi = -60,
                TransmissionInterval = "15 min",
                LastHeartbeatIST = nowIst.AddMinutes(-11),
                LastCalibrationDate = nowIst.AddDays(-10),
                HealthStatus = "Healthy"
            },
            new SensorNodeHealth
            {
                NodeId = "NODE-IN-05",
                ZoneName = "Drip Manifold Valve A",
                SensorType = "Pressure Transducer & Hall Flow Sensor",
                BatteryPercent = 64,
                SignalRssi = -78,
                TransmissionInterval = "10 min",
                LastHeartbeatIST = nowIst.AddMinutes(-9),
                LastCalibrationDate = nowIst.AddDays(-60),
                HealthStatus = "Warning"
            }
        };

        var viewModel = new TopologyViewModel
        {
            SectorMatrix = sectors,
            SensorNodes = nodes,
            FarmLayoutDescription = "Indo-Gangetic Basin Agro-Ecological Grid: 3 microclimate sectors equipped with LoRaWAN wireless telemetry nodes.",
            FarmAvgTemp = Math.Round(sectors.Average(s => s.CurrentTemp), 1),
            MaxThermalDelta = Math.Round(sectors.Max(s => s.CurrentTemp) - sectors.Min(s => s.CurrentTemp), 1)
        };

        return View(viewModel);
    }
}
