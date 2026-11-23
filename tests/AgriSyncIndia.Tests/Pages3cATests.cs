using System.Security.Claims;
using AgriSyncIndia.Controllers;
using AgriSyncIndia.Data;
using AgriSyncIndia.Models;
using AgriSyncIndia.Services;
using AgriSyncIndia.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AgriSyncIndia.Tests;

public class Pages3cATests : IAsyncLifetime
{
    SqliteConnection _conn = null!;
    AgriSyncDbContext _db = null!;

    public async Task InitializeAsync()
    {
        _conn = new SqliteConnection("Data Source=:memory:");
        _conn.Open();
        _db = new AgriSyncDbContext(new DbContextOptionsBuilder<AgriSyncDbContext>().UseSqlite(_conn).Options);
        await _db.Database.EnsureCreatedAsync();
        await DbInitializer.InitializeAsync(_db, new ServiceCollection().BuildServiceProvider());
    }

    public Task DisposeAsync() { _db.Dispose(); _conn.Dispose(); return Task.CompletedTask; }

    static ControllerContext Ctx(string? role)
    {
        var id = role == null ? new ClaimsIdentity() : new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "test");
        return new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(id) } };
    }

    ClimateController Climate(string? role) =>
        new(_db, null!, NullLogger<ClimateController>.Instance) { ControllerContext = Ctx(role) };

    AlertsController Alerts(string? role) =>
        new(_db, NullLogger<AlertsController>.Instance) { ControllerContext = Ctx(role) };

    // ---- History math

    [Theory]
    [InlineData(null, "24h")] [InlineData("7d", "7d")] [InlineData("7D", "7d")] [InlineData("x", "24h")]
    public void Range_is_normalised(string? input, string expected) => Assert.Equal(expected, HistoryMath.NormalizeRange(input));

    [Fact]
    public void Range_hours() { Assert.Equal(24, HistoryMath.RangeHours("24h")); Assert.Equal(168, HistoryMath.RangeHours("7d")); }

    [Fact]
    public void Summarize_min_avg_max_and_empty()
    {
        var s = HistoryMath.Summarize(new[] { 10.0, 20.0, 30.0 });
        Assert.Equal((10.0, 20.0, 30.0), (s!.Min, s.Avg, s.Max));
        Assert.Null(HistoryMath.Summarize(Array.Empty<double>()));
    }

    [Fact]
    public void Hourly_never_fills_missing_hours_and_keeps_zone_apart()
    {
        var t0 = new DateTime(2026, 1, 1, 10, 5, 0);
        var logs = new List<SensorLog>
        {
            new() { ZoneId = 1, Temperature = 20, VPD = 0.9, TimestampIST = t0 },
            new() { ZoneId = 2, Temperature = 25, VPD = 1.1, TimestampIST = t0 },
            new() { ZoneId = 1, Temperature = 22, VPD = 1.0, TimestampIST = t0.AddHours(1) }
        };
        var chart = HistoryMath.Hourly(logs, new[] { (1, "poly", "P"), (2, "open", "O") }, "24h");
        Assert.Equal(new[] { "10:00", "11:00" }, chart.Labels);
        Assert.Equal(new double?[] { 20, 22 }, chart.Series[0].Temp);
        Assert.Equal(new double?[] { 25, null }, chart.Series[1].Temp);
    }

    // ---- History controller

    [Fact]
    public async Task Analytics_default_is_24h_all_zones_with_real_summary()
    {
        var vm = (HistoryViewModel)((ViewResult)await Climate(null).Analytics(null, null)).Model!;
        Assert.Equal("24h", vm.Range);
        Assert.Equal(3, vm.Chart.Series.Count);
        Assert.NotNull(vm.Temp);
        var since = IstClock.Now.AddHours(-24);
        var raw = await _db.SensorLogs.Where(l => l.TimestampIST >= since).Select(l => l.Temperature).ToListAsync();
        Assert.Equal(Math.Round(raw.Min(), 1), vm.Temp!.Min);
        Assert.Equal(Math.Round(raw.Max(), 1), vm.Temp.Max);
    }

    [Fact]
    public async Task Analytics_7d_for_one_zone_and_unknown_zone_falls_back_to_all()
    {
        var one = _db.Zones.First();
        var vm = (HistoryViewModel)((ViewResult)await Climate(null).Analytics("7d", one.ZoneId)).Model!;
        Assert.Single(vm.Chart.Series);
        Assert.Equal(one.ZoneId, vm.SelectedZoneId);
        Assert.True(vm.Chart.Labels.Count > 100);
        var bad = (HistoryViewModel)((ViewResult)await Climate(null).Analytics("7d", 9999)).Model!;
        Assert.Null(bad.SelectedZoneId);
        Assert.Equal(3, bad.Chart.Series.Count);
    }

    [Fact]
    public async Task Analytics_without_logs_has_no_summary()
    {
        _db.SensorLogs.RemoveRange(_db.SensorLogs); await _db.SaveChangesAsync();
        var vm = (HistoryViewModel)((ViewResult)await Climate(null).Analytics(null, null)).Model!;
        Assert.Null(vm.Temp); Assert.Empty(vm.Chart.Labels);
    }

    [Fact]
    public async Task ExportCsv_is_a_get_file_with_header_and_rows()
    {
        var f = (FileContentResult)await Climate(null).ExportCsv("24h", null);
        var text = System.Text.Encoding.UTF8.GetString(f.FileContents);
        Assert.StartsWith("LogId,TimestampIST", text);
        Assert.True(text.Split('\n').Length > 10);
    }

    // ---- Topology

    [Fact]
    public async Task Topology_uses_db_zones_and_actuators()
    {
        var vm = (TopologyViewModel)((ViewResult)await Climate(null).Topology()).Model!;
        Assert.Equal(3, vm.Zones.Count);
        Assert.All(vm.Zones, z => Assert.True(z.Card.HasReading));
        Assert.Equal(3, vm.Zones.Sum(z => z.Actuators.Count));
    }

    [Fact]
    public async Task Topology_without_logs_shows_no_reading_not_targets()
    {
        _db.SensorLogs.RemoveRange(_db.SensorLogs); await _db.SaveChangesAsync();
        var vm = (TopologyViewModel)((ViewResult)await Climate(null).Topology()).Model!;
        Assert.All(vm.Zones, z => { Assert.False(z.Card.HasReading); Assert.Null(z.Card.Temp); Assert.Null(z.Card.Vpd); });
    }

    [Theory]
    [InlineData(null, false)] [InlineData("Viewer", false)] [InlineData("Agronomist", true)] [InlineData("Farm Manager", true)]
    public async Task Topology_toggle_only_for_operators(string? role, bool expected)
    {
        var vm = (TopologyViewModel)((ViewResult)await Climate(role).Topology()).Model!;
        Assert.Equal(expected, vm.CanToggle);
    }

    // ---- Alerts

    [Fact]
    public void Filter_normalise_and_apply()
    {
        Assert.Equal("All", AlertFilter.Normalize(null));
        Assert.Equal("Resolved", AlertFilter.Normalize("resolved"));
        Assert.Equal("All", AlertFilter.Normalize("Info"));
        var a = new[]
        {
            new ClimateAlert { Severity = "Critical" }, new ClimateAlert { Severity = "Critical", IsAcknowledged = true },
            new ClimateAlert { Severity = "Warning" }
        };
        Assert.Equal(3, AlertFilter.Apply(a, "All").Count());
        Assert.Single(AlertFilter.Apply(a, "Critical"));
        Assert.Single(AlertFilter.Apply(a, "Warning"));
        Assert.Single(AlertFilter.Apply(a, "Resolved"));
    }

    [Fact]
    public async Task Alerts_index_counts_and_filter()
    {
        var all = (AlertsViewModel)((ViewResult)await Alerts(null).Index(null)).Model!;
        Assert.Equal(3, all.Counts["All"]); Assert.Equal(1, all.Counts["Critical"]);
        Assert.Equal(1, all.Counts["Warning"]); Assert.Equal(1, all.Counts["Resolved"]);
        var res = (AlertsViewModel)((ViewResult)await Alerts(null).Index("Resolved")).Model!;
        Assert.Single(res.Alerts); Assert.True(res.Alerts[0].IsAcknowledged);
    }

    [Theory]
    [InlineData(null, false)] [InlineData("Viewer", false)] [InlineData("Agronomist", true)]
    public async Task Alerts_acknowledge_only_for_operators(string? role, bool expected)
    {
        var vm = (AlertsViewModel)((ViewResult)await Alerts(role).Index(null)).Model!;
        Assert.Equal(expected, vm.CanAcknowledge);
    }

    [Fact]
    public async Task Acknowledge_by_viewer_is_not_found_and_by_operator_resolves()
    {
        var id = _db.ClimateAlerts.First(a => !a.IsAcknowledged).AlertId;
        Assert.IsType<NotFoundResult>(await Alerts("Viewer").Acknowledge(id, "All"));
        var op = Alerts("Agronomist");
        op.TempData = new TempDataDictionary(op.HttpContext, new NoTempData());
        Assert.IsType<RedirectToActionResult>(await op.Acknowledge(id, "All"));
        Assert.True((await _db.ClimateAlerts.FindAsync(id))!.IsAcknowledged);
    }

    sealed class NoTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext c) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext c, IDictionary<string, object> v) { }
    }
}
