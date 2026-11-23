using System.ComponentModel.DataAnnotations;
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

public class Automation3cBTests : IDisposable
{
    readonly SqliteConnection _conn = new("Data Source=:memory:");
    readonly AgriSyncDbContext _db;

    public Automation3cBTests()
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

    static ClaimsPrincipal Who(int id, string role) =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Role, role) }, "test"));

    static T With<T>(T c, ClaimsPrincipal user) where T : Controller
    {
        var http = new DefaultHttpContext { User = user };
        c.ControllerContext = new ControllerContext(new ActionContext(http, new Microsoft.AspNetCore.Routing.RouteData(), new ControllerActionDescriptor()));
        c.TempData = new TempDataDictionary(http, new NoTempData());
        return c;
    }

    AutomationController Auto(ClaimsPrincipal u) => With(new AutomationController(_db, NullLogger<AutomationController>.Instance), u);

    async Task<int> SeedFan()
    {
        var zone = new Zone { Name = "Polyhouse Nursery" };
        _db.Zones.Add(zone);
        var fan = new Actuator { Zone = zone, Name = "Exhaust Fans 1 & 2" };
        _db.Actuators.Add(fan);
        await _db.SaveChangesAsync();
        return fan.ActuatorId;
    }

    static IList<ValidationResult> Check(object o)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(o, new ValidationContext(o), results, true);
        return results;
    }

    static AutomationRuleInputModel Valid() => new() { ConditionVariable = "Temperature", Operator = ">", ThresholdValue = 28, ActuatorId = 1 };

    // ---- validation ----
    [Fact] public void Valid_rule_passes() => Assert.Empty(Check(Valid()));

    [Theory]
    [InlineData("Temperature", 61, "val.thr.temp")]
    [InlineData("Relative Humidity", 101, "val.thr.hum")]
    [InlineData("Relative Humidity", -1, "val.thr.hum")]
    [InlineData("VPD", 6, "val.thr.vpd")]
    public void Threshold_out_of_range_for_metric_is_rejected(string metric, double value, string key)
    {
        var m = Valid(); m.ConditionVariable = metric; m.ThresholdValue = value;
        Assert.Contains(Check(m), r => r.ErrorMessage == key);
    }

    [Fact]
    public void Missing_threshold_actuator_and_bad_operator_are_rejected()
    {
        var m = Valid(); m.ThresholdValue = null; m.ActuatorId = null; m.Operator = "!=";
        var keys = Check(m).Select(r => r.ErrorMessage).ToList();
        Assert.Contains("val.thr.req", keys);
        Assert.Contains("val.actuator", keys);
        Assert.Contains("val.op", keys);
    }

    [Fact]
    public void Every_validation_key_is_translated_in_all_languages()
    {
        foreach (var k in new[] { "val.thr.temp", "val.thr.hum", "val.thr.vpd", "val.op", "val.actuator", "val.cl.dup", "val.cl.both", "au.ro.why", "au.act.why", "au.act.signin" })
            foreach (var lang in Localizer.Languages)
                Assert.NotEqual(k, Localizer.T(lang, k));
        Assert.Equal(Localizer.T("hi", "val.invalid"), FormText.Error("hi", "The value 'x' is not valid."));
    }

    // ---- sentence ----
    [Fact]
    public void Sentence_reads_like_a_person_wrote_it()
    {
        var r = new AutomationRule { ConditionVariable = "Temperature", Operator = ">", ThresholdValue = 28, ActionTarget = "Turn ON Exhaust Fans 1 & 2" };
        Assert.Equal("If temperature is above 28 °C, turn on exhaust fans", RuleDisplay.Sentence("en", r));
        foreach (var lang in new[] { "hi", "pa" })
        {
            var s = RuleDisplay.Sentence(lang, r);
            Assert.DoesNotContain("{", s);
            Assert.NotEqual(RuleDisplay.Sentence("en", r), s);
        }
    }

    [Fact]
    public void Sentence_handles_off_actions_and_decimal_thresholds()
    {
        var r = new AutomationRule { ConditionVariable = "VPD", Operator = "<", ThresholdValue = 0.45, ActionTarget = "Turn OFF Misting Pump High-Pressure" };
        Assert.Equal("If VPD is below 0.45 kPa, turn off misting pump", RuleDisplay.Sentence("en", r));
    }

    // ---- permission display ----
    [Fact]
    public void Viewer_sees_actuator_controls_disabled_with_a_reason()
    {
        var a = ControlAccess.ForActuators(Who(1, "Viewer"));
        Assert.False(a.Allowed); Assert.Equal("au.act.why", a.ReasonKey);
        Assert.True(ControlAccess.ForActuators(Who(1, "Farm Manager")).Allowed);
        Assert.Equal("au.act.signin", ControlAccess.ForActuators(new ClaimsPrincipal(new ClaimsIdentity())).ReasonKey);
    }

    [Fact]
    public void Rule_controls_follow_ownership_and_role()
    {
        var mine = new AutomationRule { OwnerUserId = 5 };
        var seeded = new AutomationRule();
        Assert.True(ControlAccess.ForRule(Who(5, "Viewer"), mine).Allowed);
        Assert.False(ControlAccess.ForRule(Who(5, "Viewer"), seeded).Allowed);
        Assert.True(ControlAccess.ForRule(Who(9, "Agronomist"), seeded).Allowed);
        Assert.Equal("you", ControlAccess.Owner(Who(5, "Viewer"), mine));
        Assert.Equal("seed", ControlAccess.Owner(Who(5, "Viewer"), seeded));
        Assert.Equal("user", ControlAccess.Owner(Who(6, "Viewer"), mine));
    }

    // ---- evaluator path: Viewer creates a rule, sees it, deletes it ----
    [Fact]
    public async Task Viewer_creates_sees_and_deletes_own_rule()
    {
        var fanId = await SeedFan();
        var viewer = Who(42, "Viewer");
        var form = new AutomationViewModel { NewRule = new AutomationRuleInputModel { ConditionVariable = "Temperature", Operator = ">", ThresholdValue = 28, ActuatorId = fanId, TurnOn = true } };
        Assert.IsType<RedirectToActionResult>(await Auto(viewer).CreateRule(form));

        var page = Assert.IsType<ViewResult>(await Auto(viewer).Index());
        var vm = Assert.IsType<AutomationViewModel>(page.Model);
        var rule = Assert.Single(vm.ExistingRules);
        Assert.Equal("Turn ON Exhaust Fans 1 & 2", rule.ActionTarget);
        Assert.Equal(42, rule.OwnerUserId);
        Assert.Equal("Polyhouse Nursery", RuleDisplay.ActuatorFor(vm.Actuators, rule.ActionTarget)?.Zone?.Name);
        Assert.True(ControlAccess.ForRule(viewer, rule).Allowed);

        Assert.IsType<RedirectToActionResult>(await Auto(viewer).DeleteRule(rule.RuleId));
        Assert.Empty(_db.AutomationRules);
    }

    [Fact]
    public async Task Unknown_actuator_returns_form_with_error_and_saves_nothing()
    {
        await SeedFan();
        var c = Auto(Who(42, "Viewer"));
        var form = new AutomationViewModel { NewRule = new AutomationRuleInputModel { ThresholdValue = 28, ActuatorId = 999 } };
        var result = Assert.IsType<ViewResult>(await c.CreateRule(form));
        Assert.Equal("Index", result.ViewName);
        Assert.False(c.ModelState.IsValid);
        Assert.Empty(_db.AutomationRules);
        Assert.Single(((AutomationViewModel)result.Model!).Actuators);
    }

    [Fact]
    public async Task Soil_card_says_no_reading_when_there_are_no_logs()
    {
        var vm = (AutomationViewModel)((ViewResult)await Auto(Who(1, "Viewer")).Index()).Model!;
        Assert.Null(vm.SoilMetrics.VolumetricWaterContent);
        Assert.Null(vm.SoilMetrics.Status);
    }

    // ---- clusters ----
    FarmClusterController Clusters() => With(new FarmClusterController(_db, NullLogger<FarmClusterController>.Instance), Who(3, "Viewer"));

    [Fact]
    public void Cluster_form_validation()
    {
        var bad = new CreateFarmClusterViewModel { Name = "ab", Region = "", Latitude = 120, TotalAcreage = 0, PrimaryCrops = "" };
        var keys = Check(bad).Select(r => r.ErrorMessage).ToList();
        foreach (var k in new[] { "val.cl.name", "val.cl.region", "val.cl.lat", "val.cl.acres", "val.cl.crops" }) Assert.Contains(k, keys);
        Assert.Empty(Check(new CreateFarmClusterViewModel { Name = "Karnal block", Region = "Haryana", TotalAcreage = 12, PrimaryCrops = "Wheat" }));
    }

    [Fact]
    public async Task Cluster_with_blank_coordinates_uses_delhi_and_duplicate_name_is_rejected()
    {
        var model = new CreateFarmClusterViewModel { Name = "Karnal block", Region = "Haryana", TotalAcreage = 12, PrimaryCrops = "Wheat" };
        Assert.IsType<RedirectToActionResult>(await Clusters().Create(model));
        var saved = await _db.FarmClusters.SingleAsync(c => c.Name == "Karnal block");
        Assert.Equal(28.6139, saved.Latitude);

        var c2 = Clusters();
        Assert.IsType<ViewResult>(await c2.Create(new CreateFarmClusterViewModel { Name = "KARNAL block", Region = "x", TotalAcreage = 1, PrimaryCrops = "y" }));
        Assert.Contains(c2.ModelState["Name"]!.Errors, e => e.ErrorMessage == "val.cl.dup");
    }

    [Fact]
    public async Task One_coordinate_without_the_other_is_rejected()
    {
        var c = Clusters();
        var r = await c.Create(new CreateFarmClusterViewModel { Name = "Solo coord", Region = "x", Latitude = 30, TotalAcreage = 1, PrimaryCrops = "y" });
        Assert.IsType<ViewResult>(r);
        Assert.Empty(_db.FarmClusters.Where(f => f.Name == "Solo coord"));
    }
}
