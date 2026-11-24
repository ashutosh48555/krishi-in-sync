using System.Globalization;
using AgriSyncIndia.Services;
using AgriSyncIndia.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Controllers;

public partial class ClimateController
{
    /// <summary>Zones (/Climate/Topology): one card per DB zone with its latest reading (last 3 hours) and actuators.</summary>
    [HttpGet("Topology")]
    public async Task<IActionResult> Topology(string? farm = null)
    {
        var lang = HttpContext.Lang();
        var clusters = await _context.FarmClusters.AsNoTracking().OrderBy(c => c.ClusterId).ToListAsync();
        var farms = clusters.Select(c => c.Name).ToList();
        if (farms.Count == 0) farms.Add("IARI Pusa Smart Farm - New Delhi");

        string? cookieFarm = Request.Cookies["krishi_farm"];
        var selected = !string.IsNullOrEmpty(farm) && farms.Contains(farm) ? farm
            : (!string.IsNullOrEmpty(cookieFarm) && farms.Contains(cookieFarm) ? cookieFarm : farms[0]);

        if (!string.IsNullOrEmpty(selected))
        {
            try
            {
                Response.Cookies.Append("krishi_farm", selected, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    SameSite = SameSiteMode.Lax,
                    HttpOnly = true
                });
            }
            catch { /* Ignore if headers sent */ }
        }

        var selectedCluster = clusters.FirstOrDefault(c => c.Name == selected);
        if (!string.IsNullOrEmpty(farm))
        {
            try
            {
                await _weatherService.IngestAndSyncTelemetryAsync(selectedCluster?.Latitude, selectedCluster?.Longitude);
            }
            catch { /* fallback */ }
        }

        var cutoff = IstClock.Now.AddHours(-ClimateRules.NoReadingAfterHours);
        var zones = await _context.Zones.Include(z => z.Actuators).AsNoTracking().OrderBy(z => z.ZoneId).ToListAsync();
        var vm = new TopologyViewModel
        {
            CurrentLanguage = lang,
            CanToggle = Permissions.IsOperator(User),
            SelectedFarm = selected,
            SelectedRegion = selectedCluster?.Region ?? string.Empty,
            AvailableFarms = farms
        };

        foreach (var z in zones)
        {
            var log = await _context.SensorLogs.AsNoTracking().Where(l => l.ZoneId == z.ZoneId && l.TimestampIST >= cutoff)
                .OrderByDescending(l => l.TimestampIST).FirstOrDefaultAsync();
            var key = DashboardViewModelBuilder.ZoneKey(z.Name);
            vm.Zones.Add(new TopoZone
            {
                Card = new ZoneCardViewModel
                {
                    ZoneId = z.ZoneId,
                    Key = key.Length > 0 ? key : "z" + z.ZoneId,
                    Name = z.Name,
                    DisplayName = key.Length > 0 ? Localizer.T(lang, "zone." + key) : z.Name,
                    State = ClimateRules.Status(log?.Temperature, log?.Humidity, z.TargetTemp, z.TargetHumidity),
                    HasReading = log != null,
                    Temp = log?.Temperature,
                    Humidity = log?.Humidity,
                    Vpd = log?.VPD,
                    TargetTemp = z.TargetTemp,
                    TargetHumidity = z.TargetHumidity,
                    Delta = log == null ? null : Math.Round(log.Temperature - z.TargetTemp, 1),
                    VpdBand = log == null ? null : ClimateRules.Band(log.VPD)
                },
                CropType = z.CropType,
                AreaSqMeters = z.AreaSqMeters,
                LastReading = log == null ? string.Empty : log.TimestampIST.ToString("dd MMM, HH:mm", CultureInfo.InvariantCulture),
                Actuators = z.Actuators.OrderBy(a => a.ActuatorId)
                    .Select(a => new TopoActuator(a.ActuatorId, a.Name, a.DeviceType, a.CurrentStatus == "ON", a.IsAutoMode)).ToList()
            });
        }
        return View(vm);
    }
}
