using System.Globalization;
using System.Text;
using AgriSyncIndia.Data;
using AgriSyncIndia.Models;
using AgriSyncIndia.Services;
using AgriSyncIndia.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Controllers;

[Route("[controller]")]
[Route("")]
public class ClimateController : Controller
{
    private readonly AgriSyncDbContext _context;
    private readonly IOpenMeteoService _weatherService;
    private readonly ILogger<ClimateController> _logger;

    public ClimateController(
        AgriSyncDbContext context,
        IOpenMeteoService weatherService,
        ILogger<ClimateController> logger)
    {
        _context = context;
        _weatherService = weatherService;
        _logger = logger;
    }

    /// <summary>
    /// View 1: Executive Dashboard (/Climate/Dashboard or /)
    /// </summary>
    [HttpGet]
    [HttpGet("Dashboard")]
    [Route("~/")]
    public async Task<IActionResult> Dashboard(string? farm, string? cropPreset, string? lang)
    {
        var nowIst = IstClock.Now;
        string currentLang = lang?.ToLowerInvariant() switch
        {
            "hi" => "hi",
            "pa" => "pa",
            _ => "en"
        };

        // Ensure database has zones
        var zones = await _context.Zones
            .Include(z => z.Actuators)
            .OrderBy(z => z.ZoneId)
            .ToListAsync();

        if (!zones.Any())
        {
            return View(new DashboardViewModel());
        }

        // Latest log per zone; older than NoReadingAfterHours counts as no reading.
        var freshCutoff = nowIst.AddHours(-ClimateRules.NoReadingAfterHours);
        var latestByZone = new Dictionary<int, SensorLog>();
        foreach (var z in zones)
        {
            var latest = await _context.SensorLogs
                .Where(l => l.ZoneId == z.ZoneId)
                .OrderByDescending(l => l.TimestampIST)
                .FirstOrDefaultAsync();
            if (latest != null && latest.TimestampIST >= freshCutoff)
                latestByZone[z.ZoneId] = latest;
        }

        // Ambient cards: live Open-Meteo, or last stored reading when unreachable
        var ambient = await _weatherService.GetAmbientAsync();
        double? ambientVpd = ambient == null ? null : Math.Round(ClimateRules.Vpd(ambient.Temp, ambient.Humidity), 2);
        var (vpdBadgeText, vpdBadgeClass, vpdDesc) = ambientVpd == null
            ? ("No reading yet", "bg-secondary", string.Empty)
            : _weatherService.GetVpdStatus(ambientVpd.Value);

        // Actuators
        var actuators = await _context.Actuators
            .Include(a => a.Zone)
            .OrderBy(a => a.ActuatorId)
            .Select(a => new ActuatorViewModel
            {
                ActuatorId = a.ActuatorId,
                Name = a.Name,
                ZoneId = a.ZoneId,
                ZoneName = a.Zone != null ? a.Zone.Name : "General",
                IsAutoMode = a.IsAutoMode,
                CurrentStatus = a.CurrentStatus,
                DeviceType = a.DeviceType,
                PowerRatingKw = a.PowerRatingKw,
                LastTriggered = a.LastTriggered
            })
            .ToListAsync();

        // 24-Hour Diurnal Telemetry for Chart.js
        var cutoff24h = nowIst.AddHours(-24);
        var logs24h = await _context.SensorLogs
            .Where(s => s.TimestampIST >= cutoff24h)
            .OrderBy(s => s.TimestampIST)
            .ToListAsync();

        var chartData = new ChartDataViewModel();
        var polyLogs = logs24h.Where(l => l.ZoneId == 1).ToList();
        var openLogs = logs24h.Where(l => l.ZoneId == 2).ToList();
        var dripLogs = logs24h.Where(l => l.ZoneId == 3).ToList();

        // Build synchronized labels and arrays
        foreach (var pLog in polyLogs)
        {
            chartData.Labels.Add(pLog.TimestampIST.ToString("HH:mm") + " IST");
            chartData.PolyhouseTemp.Add(pLog.Temperature);
            chartData.PolyhouseHumidity.Add(pLog.Humidity);
            chartData.PolyhouseVPD.Add(pLog.VPD);

            var oMatching = openLogs.FirstOrDefault(o => Math.Abs((o.TimestampIST - pLog.TimestampIST).TotalMinutes) < 35);
            chartData.OpenFieldTemp.Add(oMatching?.Temperature ?? Math.Round(pLog.Temperature - 3.2, 1));

            var dMatching = dripLogs.FirstOrDefault(d => Math.Abs((d.TimestampIST - pLog.TimestampIST).TotalMinutes) < 35);
            chartData.DripSectorTemp.Add(dMatching?.Temperature ?? Math.Round(pLog.Temperature - 4.4, 1));
        }

        // Zone Cards
        var zoneCards = new List<ZoneCardViewModel>();
        foreach (var z in zones)
        {
            latestByZone.TryGetValue(z.ZoneId, out var log);
            var state = ClimateRules.Status(log?.Temperature, log?.Humidity, z.TargetTemp, z.TargetHumidity);
            double? delta = log == null ? null : Math.Round(log.Temperature - z.TargetTemp, 1);
            var vpdText = log == null ? string.Empty : _weatherService.GetVpdStatus(log.VPD).BadgeText;

            zoneCards.Add(new ZoneCardViewModel
            {
                ZoneId = z.ZoneId,
                Name = z.Name,
                CropType = z.CropType,
                Status = state switch
                {
                    ZoneStatus.OnTarget => "Optimal",
                    ZoneStatus.NoReading => "No reading yet",
                    _ => "Caution"
                },
                State = state,
                HasReading = log != null,
                Delta = delta,
                VpdBand = log == null ? null : ClimateRules.Band(log.VPD),
                VpdBandText = vpdText,
                IconClass = z.IconClass,
                CurrentTemp = log?.Temperature ?? 0,
                TargetTemp = z.TargetTemp,
                CurrentHumidity = log?.Humidity ?? 0,
                TargetHumidity = z.TargetHumidity,
                CurrentVPD = log?.VPD ?? 0,
                CurrentSoilMoisture = log?.SoilMoisture ?? 0,
                Description = z.Description
            });
        }

        var zoneStates = zoneCards.Select(c => new ZoneState(c.Name, c.State, c.Delta ?? 0)).ToList();
        var polyCard = zoneCards.FirstOrDefault(c => c.Name.Contains("Polyhouse", StringComparison.OrdinalIgnoreCase));
        var advice = ClimateRules.Advice(currentLang, polyCard?.State ?? ZoneStatus.NoReading,
            polyCard?.Delta ?? 0, polyCard?.TargetTemp ?? 0);

        int activeAlertsCount = await _context.ClimateAlerts.CountAsync(a => !a.IsAcknowledged);

        // Fetch dynamic farm clusters from database
        var farmClusters = await _context.FarmClusters.OrderBy(c => c.ClusterId).ToListAsync();
        var availableFarms = farmClusters.Select(c => c.Name).ToList();
        if (!availableFarms.Any())
        {
            availableFarms = new List<string> { "IARI Pusa Smart Farm - New Delhi" };
        }

        string selectedFarmName = !string.IsNullOrEmpty(farm) && availableFarms.Contains(farm)
            ? farm
            : availableFarms.First();

        var selectedCluster = farmClusters.FirstOrDefault(c => c.Name == selectedFarmName);
        double farmLat = selectedCluster?.Latitude ?? 28.6139;
        double farmLon = selectedCluster?.Longitude ?? 77.2090;
        string farmRegion = selectedCluster?.Region ?? "Indo-Gangetic Basin";

        // Multilingual banner text (ambient values come from Open-Meteo or the stored fallback)
        string tempText = ambient == null ? "—" : ambient.Temp.ToString("0.0", CultureInfo.InvariantCulture);
        string rhText = ambient == null ? "—" : ambient.Humidity.ToString("0.0", CultureInfo.InvariantCulture);
        string windText = ambient == null ? "—" : ambient.WindSpeed.ToString("0.0", CultureInfo.InvariantCulture);
        string pressureHi = ambient?.Pressure is double p ? $" | दबाव: {p:0} hPa" : "";
        string asOf = ambient == null ? "" : ambient.TimestampIst.ToString("dd MMM, HH:mm") + " IST";
        string staleNote = ambient is { IsStale: true } ? $" | Open-Meteo unreachable, last stored reading from {asOf}" : "";
        string weatherBanner = currentLang switch
        {
            "hi" => $"लाइव मौसम प्रसारण: {selectedFarmName} ({farmRegion} - {farmLat:F2}°N, {farmLon:F2}°E) | तापमान: {tempText}°C, आर्द्रता: {rhText}%{pressureHi} | IST समय: {nowIst:dd MMM yyyy, HH:mm:ss} IST{staleNote}",
            "pa" => $"ਲਾਈਵ ਮੌਸਮ ਟੈਲੀਮੈਟਰੀ: {selectedFarmName} ({farmRegion} - {farmLat:F2}°N, {farmLon:F2}°E) | ਤਾਪਮਾਨ: {tempText}°C, ਨਮੀ: {rhText}% | IST: {nowIst:dd MMM yyyy, HH:mm:ss} IST{staleNote}",
            _ => $"Live Agromet Telemetry: {selectedFarmName} ({farmRegion} - {farmLat:F2}°N, {farmLon:F2}°E) | Ambient: {tempText}°C | RH: {rhText}% | Wind: {windText} km/h | IST: {nowIst:dd MMM yyyy, HH:mm:ss} IST{staleNote}"
        };

        var viewModel = new DashboardViewModel
        {
            SelectedFarm = selectedFarmName,
            AvailableFarms = availableFarms,
            SelectedCropPreset = string.IsNullOrEmpty(cropPreset) ? "Pusa Basmati 1121" : cropPreset,
            CurrentLanguage = currentLang,
            LiveIstTimestamp = nowIst.ToString("dd MMM yyyy | HH:mm:ss") + " IST",
            WeatherBannerText = weatherBanner,
            AmbientTemp = ambient?.Temp,
            AmbientHumidity = ambient?.Humidity,
            AmbientWindSpeed = ambient?.WindSpeed,
            SurfacePressure = ambient?.Pressure,
            AmbientIsStale = ambient?.IsStale ?? false,
            AmbientAsOf = asOf,
            Headline = ClimateRules.Headline(currentLang, zoneStates),
            Advice = advice.Text,
            AdviceReason = advice.Reason,
            PrimaryTemp = ambient?.Temp,
            PrimaryHumidity = ambient?.Humidity,
            PrimaryVPD = ambientVpd,
            VpdBadgeText = vpdBadgeText,
            VpdBadgeCssClass = vpdBadgeClass,
            VpdDescription = vpdDesc,
            SolarRadiation = latestByZone.Values.FirstOrDefault(l => l.ZoneId == polyCard?.ZoneId)?.SolarRadiation ?? 0,
            ZoneCards = zoneCards,
            Actuators = actuators,
            ChartData = chartData,
            ActiveAlertsCount = activeAlertsCount
        };

        return View(viewModel);
    }

    /// <summary>
    /// Actuator manual toggle action.
    /// </summary>
    [HttpPost("ToggleActuator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActuator(int actuatorId, string? returnUrl)
    {
        await _weatherService.ToggleActuatorAsync(actuatorId);
        TempData["SuccessMessage"] = $"Actuator #{actuatorId} status successfully toggled.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Dashboard));
    }

    /// <summary>
    /// Ingests live telemetry from Open-Meteo REST API.
    /// </summary>
    [HttpPost("RefreshTelemetry")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshTelemetry(string? returnUrl)
    {
        try
        {
            var logs = await _weatherService.IngestAndSyncTelemetryAsync();
            if (logs.Count > 0)
                TempData["SuccessMessage"] = $"Readings refreshed from Open-Meteo for {logs.Count} zones.";
            else
                TempData["ErrorMessage"] = "Could not reach Open-Meteo. Showing the last stored readings. Try again in a minute.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing telemetry");
            TempData["ErrorMessage"] = "Could not refresh readings. Showing the last stored readings. Try again in a minute.";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Dashboard));
    }

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

    /// <summary>
    /// View 3: Historical Analytics (/Climate/Analytics)
    /// </summary>
    [HttpGet("Analytics")]
    public async Task<IActionResult> Analytics(DateTime? startDate, DateTime? endDate, int? zoneId)
    {
        var nowIst = IstClock.Now;
        var sDate = startDate ?? nowIst.AddDays(-2);
        var eDate = endDate ?? nowIst;

        var zones = await _context.Zones.OrderBy(z => z.ZoneId).ToListAsync();

        var query = _context.SensorLogs.AsNoTracking()
            .Where(s => s.TimestampIST >= sDate && s.TimestampIST <= eDate.AddDays(1));

        if (zoneId.HasValue && zoneId.Value > 0)
        {
            query = query.Where(s => s.ZoneId == zoneId.Value);
        }

        var records = await query
            .OrderBy(s => s.TimestampIST)
            .ToListAsync();

        var viewModel = new AnalyticsViewModel
        {
            StartDate = sDate,
            EndDate = eDate,
            SelectedZoneId = zoneId,
            AvailableZones = zones,
            HistoricalRecords = records.OrderByDescending(r => r.TimestampIST).Take(100).ToList()
        };

        if (records.Any())
        {
            viewModel.AvgTemp = Math.Round(records.Average(r => r.Temperature), 1);
            viewModel.MinTemp = Math.Round(records.Min(r => r.Temperature), 1);
            viewModel.MaxTemp = Math.Round(records.Max(r => r.Temperature), 1);
            viewModel.AvgHumidity = Math.Round(records.Average(r => r.Humidity), 1);
            viewModel.AvgVPD = Math.Round(records.Average(r => r.VPD), 2);
            viewModel.AvgSoilMoisture = Math.Round(records.Average(r => r.SoilMoisture), 1);

            // Group or sample records for chart if too many
            var sampledRecords = records.Count > 100
                ? records.Where((_, idx) => idx % (records.Count / 50) == 0).ToList()
                : records;

            foreach (var rec in sampledRecords)
            {
                viewModel.ChartTimestamps.Add(rec.TimestampIST.ToString("dd MMM HH:mm"));
                viewModel.ChartTemperatures.Add(rec.Temperature);
                viewModel.ChartHumidities.Add(rec.Humidity);
                viewModel.ChartVPDs.Add(rec.VPD);
                viewModel.ChartSoilMoistures.Add(rec.SoilMoisture);
            }
        }

        return View(viewModel);
    }

    /// <summary>
    /// CSV Export of historical telemetry records.
    /// </summary>
    [HttpGet("ExportCsv")]
    public async Task<IActionResult> ExportCsv(DateTime? startDate, DateTime? endDate, int? zoneId)
    {
        var nowIst = IstClock.Now;
        var sDate = startDate ?? nowIst.AddDays(-7);
        var eDate = endDate ?? nowIst;

        var query = _context.SensorLogs
            .Include(s => s.Zone)
            .AsNoTracking()
            .Where(s => s.TimestampIST >= sDate && s.TimestampIST <= eDate.AddDays(1));

        if (zoneId.HasValue && zoneId.Value > 0)
        {
            query = query.Where(s => s.ZoneId == zoneId.Value);
        }

        var data = await query.OrderBy(s => s.TimestampIST).ToListAsync();

        var builder = new StringBuilder();
        builder.AppendLine("LogId,TimestampIST,ZoneId,ZoneName,Temperature_C,Humidity_Pct,VPD_kPa,SoilMoisture_Pct,SolarRadiation_Wm2,SoilTemp_C,WindSpeed_kmh");

        foreach (var item in data)
        {
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},\"{3}\",{4},{5},{6},{7},{8},{9},{10}",
                item.LogId,
                item.TimestampIST.ToString("yyyy-MM-dd HH:mm:ss"),
                item.ZoneId,
                item.Zone?.Name ?? "Unknown",
                item.Temperature,
                item.Humidity,
                item.VPD,
                item.SoilMoisture,
                item.SolarRadiation,
                item.SoilTemperature,
                item.WindSpeed));
        }

        byte[] buffer = Encoding.UTF8.GetBytes(builder.ToString());
        string fileName = $"AgriSync_Telemetry_{nowIst:yyyyMMdd_HHmm}.csv";

        return File(buffer, "text/csv", fileName);
    }
}
