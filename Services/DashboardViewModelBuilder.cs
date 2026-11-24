using System.Globalization;
using AgriSyncIndia.Data;
using AgriSyncIndia.Models;
using AgriSyncIndia.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Services;

/// <summary>Builds the view model for Home and Climate/Dashboard from the DB and Open-Meteo. No invented numbers.</summary>
public class DashboardViewModelBuilder
{
    private const string DefaultFarm = "IARI Pusa Smart Farm - New Delhi";
    private readonly AgriSyncDbContext _context;
    private readonly IOpenMeteoService _weather;

    public DashboardViewModelBuilder(AgriSyncDbContext context, IOpenMeteoService weather)
    {
        _context = context;
        _weather = weather;
    }

    public static string ZoneKey(string name) =>
        name.Contains("Polyhouse", StringComparison.OrdinalIgnoreCase) ? "poly"
        : name.Contains("Open", StringComparison.OrdinalIgnoreCase) ? "open"
        : name.Contains("Drip", StringComparison.OrdinalIgnoreCase) ? "drip" : "";

    public async Task<DashboardViewModel> BuildAsync(string lang, string? farm = null, Microsoft.AspNetCore.Http.HttpContext? httpContext = null)
    {
        var nowIst = IstClock.Now;
        var zones = await _context.Zones.Include(z => z.Actuators).AsNoTracking().OrderBy(z => z.ZoneId).ToListAsync();
        var clusters = await _context.FarmClusters.AsNoTracking().OrderBy(c => c.ClusterId).ToListAsync();
        var farms = clusters.Select(c => c.Name).ToList();
        if (farms.Count == 0) farms.Add(DefaultFarm);

        string? cookieFarm = httpContext?.Request.Cookies["krishi_farm"];
        var selected = !string.IsNullOrEmpty(farm) && farms.Contains(farm) ? farm
            : (!string.IsNullOrEmpty(cookieFarm) && farms.Contains(cookieFarm) ? cookieFarm : farms[0]);

        if (httpContext != null && !string.IsNullOrEmpty(selected))
        {
            try
            {
                httpContext.Response.Cookies.Append("krishi_farm", selected, new Microsoft.AspNetCore.Http.CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                    HttpOnly = true
                });
            }
            catch { /* Ignore if headers sent */ }
        }

        var selectedCluster = clusters.FirstOrDefault(c => c.Name == selected);
        string place = !string.IsNullOrWhiteSpace(selectedCluster?.Region) ? selectedCluster.Region
            : (selected.Contains(" - ") ? selected[(selected.LastIndexOf(" - ", StringComparison.Ordinal) + 3)..] : selected);

        var vm = new DashboardViewModel
        {
            CurrentLanguage = lang,
            SelectedFarm = selected,
            SelectedRegion = selectedCluster?.Region ?? string.Empty,
            Place = place,
            AvailableFarms = farms,
            ActiveAlertsCount = await _context.ClimateAlerts.CountAsync(a => !a.IsAcknowledged)
        };

        var ambient = await _weather.GetAmbientAsync(selectedCluster?.Latitude, selectedCluster?.Longitude);
        if (ambient != null)
        {
            vm.AmbientTemp = ambient.Temp;
            vm.AmbientHumidity = ambient.Humidity;
            vm.AmbientVpd = Math.Round(ClimateRules.Vpd(ambient.Temp, ambient.Humidity), 2);
            vm.AmbientIsStale = ambient.IsStale;
            vm.AmbientAsOf = ambient.TimestampIst.ToString("dd MMM, HH:mm", CultureInfo.InvariantCulture) + " IST";
        }

        var latest = await LatestByZone(zones, nowIst);
        vm.ZoneCards = zones.Select(z => Card(z, latest.GetValueOrDefault(z.ZoneId), lang)).ToList();
        if (latest.Count > 0) vm.UpdatedAt = latest.Values.Max(l => l.TimestampIST).ToString("HH:mm", CultureInfo.InvariantCulture);

        var states = vm.ZoneCards.Select(c => new ZoneState(c.DisplayName, c.State, c.Delta ?? 0)).ToList();
        var poly = vm.ZoneCards.FirstOrDefault(c => c.Key == "poly");
        var advice = ClimateRules.Advice(lang, poly?.State ?? ZoneStatus.NoReading, poly?.Delta ?? 0, poly?.TargetTemp ?? 0);
        vm.Headline = ClimateRules.Headline(lang, states);
        vm.Advice = advice.Text;
        vm.AdviceReason = advice.Reason;

        vm.Chart = await BuildChart(zones, vm.ZoneCards, nowIst);
        vm.Alerts = await Alerts();
        return vm;
    }

    // Latest log per zone; older than NoReadingAfterHours counts as no reading.
    private async Task<Dictionary<int, SensorLog>> LatestByZone(List<Zone> zones, DateTime nowIst)
    {
        var cutoff = nowIst.AddHours(-ClimateRules.NoReadingAfterHours);
        var result = new Dictionary<int, SensorLog>();
        foreach (var z in zones)
        {
            var log = await _context.SensorLogs.AsNoTracking().Where(l => l.ZoneId == z.ZoneId)
                .OrderByDescending(l => l.TimestampIST).FirstOrDefaultAsync();
            if (log != null && log.TimestampIST >= cutoff) result[z.ZoneId] = log;
        }
        return result;
    }

    private static ZoneCardViewModel Card(Zone z, SensorLog? log, string lang)
    {
        var key = ZoneKey(z.Name);
        var act = z.Actuators.OrderBy(a => a.ActuatorId).FirstOrDefault();
        return new ZoneCardViewModel
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
            VpdBand = log == null ? null : ClimateRules.Band(log.VPD),
            ActuatorName = act?.Name ?? string.Empty,
            ActuatorOn = act == null ? null : act.CurrentStatus == "ON"
        };
    }

    private async Task<ChartDataViewModel> BuildChart(List<Zone> zones, List<ZoneCardViewModel> cards, DateTime nowIst)
    {
        var since = nowIst.AddHours(-24);
        var logs = await _context.SensorLogs.AsNoTracking().Where(s => s.TimestampIST >= since)
            .OrderBy(s => s.TimestampIST).ToListAsync();
        DateTime Hour(DateTime t) => new(t.Year, t.Month, t.Day, t.Hour, 0, 0);
        var hours = logs.Select(l => Hour(l.TimestampIST)).Distinct().OrderBy(h => h).ToList();
        var chart = new ChartDataViewModel { Labels = hours.Select(h => h.ToString("HH:mm", CultureInfo.InvariantCulture)).ToList() };
        foreach (var z in zones)
        {
            var byHour = logs.Where(l => l.ZoneId == z.ZoneId).GroupBy(l => Hour(l.TimestampIST))
                .ToDictionary(g => g.Key, g => g.Last());
            var card = cards.First(c => c.ZoneId == z.ZoneId);
            chart.Series.Add(new ChartSeriesViewModel
            {
                Key = card.Key,
                Name = card.DisplayName,
                Temp = hours.Select(h => byHour.TryGetValue(h, out var l) ? (double?)Math.Round(l.Temperature, 1) : null).ToList(),
                Vpd = hours.Select(h => byHour.TryGetValue(h, out var l) ? (double?)Math.Round(l.VPD, 2) : null).ToList()
            });
        }
        return chart;
    }

    private async Task<List<AlertItemViewModel>> Alerts() =>
        (await _context.ClimateAlerts.AsNoTracking().Where(a => !a.IsAcknowledged)
            .OrderByDescending(a => a.TimestampIST).Take(4).ToListAsync())
        .Select(a => new AlertItemViewModel
        {
            Severity = a.Severity,
            Title = a.Title,
            Advice = string.IsNullOrWhiteSpace(a.AgronomistRecommendation) ? a.Message : a.AgronomistRecommendation,
            TimeText = a.TimestampIST.ToString("dd MMM, HH:mm", CultureInfo.InvariantCulture)
        }).ToList();
}
