using System.Security.Claims;
using AgriSyncIndia.Controllers;
using AgriSyncIndia.Data;
using AgriSyncIndia.Models;
using AgriSyncIndia.Services;
using AgriSyncIndia.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AgriSyncIndia.Tests;

public class OwnershipTests : IDisposable
{
    readonly SqliteConnection _conn = new("Data Source=:memory:");
    readonly AgriSyncDbContext _db;

    public OwnershipTests()
    {
        _conn.Open();
        _db = new AgriSyncDbContext(new DbContextOptionsBuilder<AgriSyncDbContext>().UseSqlite(_conn).Options);
        _db.Database.EnsureCreated();
    }

    public void Dispose() { _db.Dispose(); _conn.Dispose(); }

    sealed class NoTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext c) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext c, IDictionary<string, object> v) { }
    }

    sealed class FakeWeather : IOpenMeteoService
    {
        public int Toggled;
        public Task ToggleActuatorAsync(int id) { Toggled++; return Task.CompletedTask; }
        public Task<List<SensorLog>> IngestAndSyncTelemetryAsync(double? latitude = null, double? longitude = null) => Task.FromResult(new List<SensorLog>());
        public Task<OpenMeteoResponse?> FetchLiveWeatherAsync(double latitude = 28.6139, double longitude = 77.2090) => Task.FromResult<OpenMeteoResponse?>(null);
        public Task<AmbientReading?> GetAmbientAsync(double? latitude = null, double? longitude = null) => Task.FromResult<AmbientReading?>(null);
        public double CalculateVPD(double t, double rh) => 0;
        public (string BadgeText, string BadgeCssClass, string Description) GetVpdStatus(double v) => ("", "", "");
        public Task EvaluateAutomationRulesAsync(List<SensorLog> l) => Task.CompletedTask;
    }

    static T With<T>(T c, int userId, string role) where T : Controller
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Role, role)
            }, "test"))
        };
        c.ControllerContext = new ControllerContext(new ActionContext(http, new Microsoft.AspNetCore.Routing.RouteData(), new ControllerActionDescriptor()));
        c.TempData = new TempDataDictionary(http, new NoTempData());
        return c;
    }

    AutomationController Auto(int uid, string role) => With(new AutomationController(_db, NullLogger<AutomationController>.Instance), uid, role);

    async Task<AutomationRule> Rule(int? owner)
    {
        var r = new AutomationRule { ThresholdValue = 1, OwnerUserId = owner };
        _db.AutomationRules.Add(r);
        await _db.SaveChangesAsync();
        return r;
    }

    [Fact]
    public async Task Create_records_owner_for_viewer()
    {
        var c = Auto(42, "Viewer");
        var model = new AutomationViewModel { NewRule = new AutomationRuleInputModel { ConditionVariable = "Temperature", Operator = ">", ThresholdValue = 30, ActionTarget = "Turn ON Exhaust Fans" } };
        Assert.IsType<RedirectToActionResult>(await c.CreateRule(model));
        Assert.Equal(42, (await _db.AutomationRules.SingleAsync()).OwnerUserId);
    }

    [Fact]
    public async Task Viewer_can_delete_and_toggle_own_rule()
    {
        var r = await Rule(7);
        Assert.IsType<RedirectToActionResult>(await Auto(7, "Viewer").ToggleRule(r.RuleId));
        Assert.IsType<RedirectToActionResult>(await Auto(7, "Viewer").DeleteRule(r.RuleId));
        Assert.Empty(_db.AutomationRules);
    }

    [Fact]
    public async Task Viewer_gets_404_on_someone_elses_or_seeded_rule_and_nothing_changes()
    {
        var other = await Rule(8);
        var seeded = await Rule(null);
        foreach (var id in new[] { other.RuleId, seeded.RuleId })
        {
            Assert.IsType<NotFoundResult>(await Auto(7, "Viewer").DeleteRule(id));
            Assert.IsType<NotFoundResult>(await Auto(7, "Viewer").ToggleRule(id));
        }
        Assert.Equal(2, await _db.AutomationRules.CountAsync());
    }

    [Theory]
    [InlineData("Farm Manager")]
    [InlineData("Agronomist")]
    public async Task Operator_roles_can_modify_any_rule(string role)
    {
        var r = await Rule(null);
        Assert.IsType<RedirectToActionResult>(await Auto(1, role).DeleteRule(r.RuleId));
        Assert.Empty(_db.AutomationRules);
    }

    [Fact]
    public async Task Unknown_rule_is_404() =>
        Assert.IsType<NotFoundResult>(await Auto(1, "Agronomist").DeleteRule(999));

    [Fact]
    public async Task Alert_acknowledge_needs_operator_role()
    {
        var a = new ClimateAlert { Title = "t" };
        _db.ClimateAlerts.Add(a);
        await _db.SaveChangesAsync();
        var viewer = With(new AlertsController(_db, NullLogger<AlertsController>.Instance), 7, "Viewer");
        Assert.IsType<NotFoundResult>(await viewer.Acknowledge(a.AlertId, null));
        Assert.False((await _db.ClimateAlerts.SingleAsync()).IsAcknowledged);
        var op = With(new AlertsController(_db, NullLogger<AlertsController>.Instance), 1, "Agronomist");
        Assert.IsType<RedirectToActionResult>(await op.Acknowledge(a.AlertId, null));
        Assert.True((await _db.ClimateAlerts.SingleAsync()).IsAcknowledged);
        Assert.IsType<NotFoundResult>(await op.Acknowledge(999, null));
    }

    [Fact]
    public async Task Actuator_toggle_needs_operator_role()
    {
        var zone = new Zone { Name = "z" };
        _db.Zones.Add(zone);
        _db.Actuators.Add(new Actuator { Zone = zone, Name = "fan" });
        await _db.SaveChangesAsync();
        var id = (await _db.Actuators.SingleAsync()).ActuatorId;
        var w = new FakeWeather();
        Assert.IsType<NotFoundResult>(await With(new ClimateController(_db, w, NullLogger<ClimateController>.Instance), 7, "Viewer").ToggleActuator(id, null));
        Assert.Equal(0, w.Toggled);
        Assert.IsType<RedirectToActionResult>(await With(new ClimateController(_db, w, NullLogger<ClimateController>.Instance), 1, "Farm Manager").ToggleActuator(id, null));
        Assert.Equal(1, w.Toggled);
    }
}
