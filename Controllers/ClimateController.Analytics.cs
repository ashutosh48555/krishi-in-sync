using System.Globalization;
using System.Text;
using AgriSyncIndia.Models;
using AgriSyncIndia.Services;
using AgriSyncIndia.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Controllers;

/// <summary>Pure helpers for the History page, kept apart from EF so they are easy to test.</summary>
public static class HistoryMath
{
    public static string NormalizeRange(string? range) => string.Equals(range, "7d", StringComparison.OrdinalIgnoreCase) ? "7d" : "24h";

    public static int RangeHours(string range) => NormalizeRange(range) == "7d" ? 7 * 24 : 24;

    public static Stat? Summarize(IReadOnlyCollection<double> v, int decimals = 1) =>
        v.Count == 0 ? null : new Stat(Math.Round(v.Min(), decimals), Math.Round(v.Average(), decimals), Math.Round(v.Max(), decimals));

    /// <summary>Hourly buckets, oldest first. An hour with no log for a zone stays null (never filled in).</summary>
    public static ChartDataViewModel Hourly(IEnumerable<SensorLog> logs, IEnumerable<(int Id, string Key, string Name)> zones, string range)
    {
        var list = logs.OrderBy(l => l.TimestampIST).ToList();
        DateTime Hour(DateTime t) => new(t.Year, t.Month, t.Day, t.Hour, 0, 0);
        var hours = list.Select(l => Hour(l.TimestampIST)).Distinct().OrderBy(h => h).ToList();
        var fmt = NormalizeRange(range) == "7d" ? "dd MMM HH:mm" : "HH:mm";
        var chart = new ChartDataViewModel { Labels = hours.Select(h => h.ToString(fmt, CultureInfo.InvariantCulture)).ToList() };
        foreach (var (id, key, name) in zones)
        {
            var byHour = list.Where(l => l.ZoneId == id).GroupBy(l => Hour(l.TimestampIST)).ToDictionary(g => g.Key, g => g.Last());
            chart.Series.Add(new ChartSeriesViewModel
            {
                Key = key,
                Name = name,
                Temp = hours.Select(h => byHour.TryGetValue(h, out var l) ? (double?)Math.Round(l.Temperature, 1) : null).ToList(),
                Vpd = hours.Select(h => byHour.TryGetValue(h, out var l) ? (double?)Math.Round(l.VPD, 2) : null).ToList()
            });
        }
        return chart;
    }
}

public partial class ClimateController
{
    /// <summary>History (/Climate/Analytics): range 24h or 7d, optional zone.</summary>
    [HttpGet("Analytics")]
    public async Task<IActionResult> Analytics(string? range, int? zoneId)
    {
        var lang = HttpContext.Lang();
        var r = HistoryMath.NormalizeRange(range);
        var zones = await _context.Zones.AsNoTracking().OrderBy(z => z.ZoneId).ToListAsync();
        var chosen = zones.FirstOrDefault(z => z.ZoneId == zoneId);
        var shown = chosen != null ? new List<Zone> { chosen } : zones;

        var since = IstClock.Now.AddHours(-HistoryMath.RangeHours(r));
        var ids = shown.Select(z => z.ZoneId).ToList();
        var logs = await _context.SensorLogs.AsNoTracking()
            .Where(l => l.TimestampIST >= since && ids.Contains(l.ZoneId)).OrderBy(l => l.TimestampIST).ToListAsync();

        (int, string, string) Series(Zone z)
        {
            var key = DashboardViewModelBuilder.ZoneKey(z.Name);
            return (z.ZoneId, key.Length > 0 ? key : "z" + z.ZoneId, key.Length > 0 ? Localizer.T(lang, "zone." + key) : z.Name);
        }
        var series = shown.Select(Series).ToList();

        return View(new HistoryViewModel
        {
            CurrentLanguage = lang,
            Range = r,
            SelectedZoneId = chosen?.ZoneId,
            Zones = zones.Select(z => new ZoneOption(z.ZoneId, Series(z).Item3)).ToList(),
            SampleCount = logs.Count,
            Temp = HistoryMath.Summarize(logs.Select(l => l.Temperature).ToList()),
            Humidity = HistoryMath.Summarize(logs.Select(l => l.Humidity).ToList()),
            Vpd = HistoryMath.Summarize(logs.Select(l => l.VPD).ToList(), 2),
            Chart = HistoryMath.Hourly(logs, series, r),
            Selected = chosen != null ? series[0].Item2 : "all"
        });
    }

    /// <summary>CSV of the stored readings for the same range and zone as the page. Read-only.</summary>
    [HttpGet("ExportCsv")]
    public async Task<IActionResult> ExportCsv(string? range, int? zoneId)
    {
        var nowIst = IstClock.Now;
        var since = nowIst.AddHours(-HistoryMath.RangeHours(HistoryMath.NormalizeRange(range)));
        var query = _context.SensorLogs.Include(s => s.Zone).AsNoTracking().Where(s => s.TimestampIST >= since);
        if (zoneId.HasValue) query = query.Where(s => s.ZoneId == zoneId.Value);
        var data = await query.OrderBy(s => s.TimestampIST).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("LogId,TimestampIST,ZoneId,ZoneName,Temperature_C,Humidity_Pct,VPD_kPa,SoilMoisture_Pct,SolarRadiation_Wm2,SoilTemp_C,WindSpeed_kmh");
        foreach (var i in data)
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:yyyy-MM-dd HH:mm:ss},{2},\"{3}\",{4},{5},{6},{7},{8},{9},{10}",
                i.LogId, i.TimestampIST, i.ZoneId, i.Zone?.Name ?? "Unknown", i.Temperature, i.Humidity, i.VPD, i.SoilMoisture, i.SolarRadiation, i.SoilTemperature, i.WindSpeed));

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"KrishiSync_History_{nowIst:yyyyMMdd_HHmm}.csv");
    }
}
