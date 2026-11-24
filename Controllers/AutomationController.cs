using Microsoft.AspNetCore.Authorization;
using AgriSyncIndia.Services;
using AgriSyncIndia.Data;
using AgriSyncIndia.Models;
using AgriSyncIndia.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Controllers;

[Route("[controller]")]
public class AutomationController : Controller
{
    private readonly AgriSyncDbContext _context;
    private readonly ILogger<AutomationController> _logger;

    public AutomationController(AgriSyncDbContext context, ILogger<AutomationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private async Task<AutomationViewModel> Load(AutomationRuleInputModel? input = null)
    {
        // Latest Drip Sector reading. No log means no reading: the view says so instead of showing a default.
        var drip = await _context.Zones.AsNoTracking().FirstOrDefaultAsync(z => z.Name.Contains("Drip"));
        var log = drip == null ? null : await _context.SensorLogs.AsNoTracking()
            .Where(s => s.ZoneId == drip.ZoneId).OrderByDescending(s => s.TimestampIST).FirstOrDefaultAsync();

        return new AutomationViewModel
        {
            SoilMetrics = new SoilHealthMetrics { VolumetricWaterContent = log?.SoilMoisture, RootZoneTemperature = log?.SoilTemperature },
            ExistingRules = await _context.AutomationRules.AsNoTracking().OrderByDescending(r => r.RuleId).ToListAsync(),
            Actuators = await _context.Actuators.AsNoTracking().Include(a => a.Zone).OrderBy(a => a.ActuatorId).ToListAsync(),
            NewRule = input ?? new AutomationRuleInputModel()
        };
    }

    private void Flash(string key, params object[] args) =>
        TempData["SuccessMessage"] = string.Format(Localizer.T(HttpContext.Lang(), key), args);

    [HttpGet]
    [HttpGet("Index")]
    public async Task<IActionResult> Index() => View(await Load());

    /// <summary>Create a new IF-THEN rule. Validation messages are translation keys (see AutomationRuleInputModel).</summary>
    [HttpPost("CreateRule")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRule(AutomationViewModel model)
    {
        var input = model.NewRule;
        var actionTarget = input.ActionTarget;
        if (input.ActuatorId is int actuatorId)
        {
            var actuator = await _context.Actuators.AsNoTracking().FirstOrDefaultAsync(a => a.ActuatorId == actuatorId);
            if (actuator == null) ModelState.AddModelError("NewRule.ActuatorId", "val.actuator");
            else actionTarget = RuleDisplay.ActionTargetFor(actuator, input.TurnOn);
        }

        if (!ModelState.IsValid) return View("Index", await Load(input));

        var rule = new AutomationRule
        {
            ConditionVariable = input.ConditionVariable,
            Operator = input.Operator,
            ThresholdValue = input.ThresholdValue ?? 0,
            ActionTarget = actionTarget,
            IsActive = input.IsActive,
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            OwnerUserId = Permissions.UserId(User),
            CreatedAt = IstClock.Now
        };

        _context.AutomationRules.Add(rule);
        await _context.SaveChangesAsync();
        Flash("au.t.added");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleRule")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRule(int ruleId)
    {
        var rule = await _context.AutomationRules.FindAsync(ruleId);
        // 404 (not 403) for records the caller may not touch, so IDs cannot be probed.
        if (rule == null || !Permissions.CanModify(User, rule)) return NotFound();
        rule.IsActive = !rule.IsActive;
        await _context.SaveChangesAsync();
        Flash(rule.IsActive ? "au.t.resumed" : "au.t.paused");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DeleteRule")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRule(int ruleId)
    {
        var rule = await _context.AutomationRules.FindAsync(ruleId);
        if (rule == null || !Permissions.CanModify(User, rule)) return NotFound();
        _context.AutomationRules.Remove(rule);
        await _context.SaveChangesAsync();
        Flash("au.t.deleted");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Evaluator / Demo helper: Simulates a high-humidity event, triggers rules & updates actuators.</summary>
    [HttpPost("SimulateAnomaly")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SimulateAnomaly()
    {
        if (!Permissions.IsOperator(User)) return NotFound();
        var zone = await _context.Zones.FirstOrDefaultAsync();
        if (zone == null) return NotFound();

        var anomalyLog = new SensorLog
        {
            ZoneId = zone.ZoneId,
            Temperature = 28.5,
            Humidity = 89.0,
            VPD = ClimateRules.Vpd(28.5, 89.0),
            SoilMoisture = 55.0,
            SolarRadiation = 320,
            SoilTemperature = 22.0,
            WindSpeed = 0.2,
            TimestampIST = IstClock.Now
        };
        _context.SensorLogs.Add(anomalyLog);
        await _context.SaveChangesAsync();

        var weather = HttpContext.RequestServices.GetService<IOpenMeteoService>();
        if (weather != null)
        {
            await weather.EvaluateAutomationRulesAsync(new List<SensorLog> { anomalyLog });
        }

        Flash("au.t.anomaly");
        return RedirectToAction(nameof(Index));
    }
}
