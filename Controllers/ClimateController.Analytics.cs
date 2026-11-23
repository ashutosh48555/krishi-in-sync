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
