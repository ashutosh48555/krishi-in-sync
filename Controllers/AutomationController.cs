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

    /// <summary>
    /// View 4: Automation Rules & Soil Health (/Automation/Index or /Automation)
    /// </summary>
    [HttpGet]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var rules = await _context.AutomationRules
            .OrderByDescending(r => r.RuleId)
            .ToListAsync();

        var actuators = await _context.Actuators
            .Include(a => a.Zone)
            .OrderBy(a => a.ActuatorId)
            .ToListAsync();

        // Get latest soil telemetry from Drip Sector or average
        var latestDripLog = await _context.SensorLogs
            .Where(s => s.ZoneId == 3)
            .OrderByDescending(s => s.TimestampIST)
            .FirstOrDefaultAsync();

        double currentMoisture = latestDripLog?.SoilMoisture ?? 48.5;
        double currentSoilTemp = latestDripLog?.SoilTemperature ?? 22.8;

        string irrigationStatus = currentMoisture switch
        {
            < 30.0 => "Water Stress Deficit - Immediate Irrigation Required",
            >= 30.0 and < 45.0 => "Moderate Depletion - Scheduled Irrigation Cycle Due",
            >= 45.0 and <= 75.0 => "Optimal Field Capacity - Healthy Root Respiration",
            _ => "Saturated - Risk of Root Hypoxia & Waterlogging"
        };

        var soilMetrics = new SoilHealthMetrics
        {
            VolumetricWaterContent = currentMoisture,
            RootZoneTemperature = currentSoilTemp,
            FieldCapacity = 68.0,
            PermanentWiltingPoint = 22.0,
            PlantAvailableWater = Math.Round(Math.Max(0.0, Math.Min(100.0, (currentMoisture - 22.0) / (68.0 - 22.0) * 100.0)), 1),
            IrrigationStatus = irrigationStatus,
            SoilTextureType = "Indo-Gangetic Loamy Alluvium (ICAR Soil Survey)",
            SalinityEC = 0.74
        };

        var model = new AutomationViewModel
        {
            SoilMetrics = soilMetrics,
            ExistingRules = rules,
            Actuators = actuators,
            NewRule = new AutomationRuleInputModel
            {
                ConditionVariable = "Relative Humidity",
                Operator = ">",
                ThresholdValue = 75.0,
                ActionTarget = "Turn ON Exhaust Fans",
                IsActive = true,
                Description = "IF RH > 75% THEN Turn ON Exhaust Fans"
            }
        };

        return View(model);
    }

    /// <summary>
    /// Create a new IF-THEN automation rule.
    /// Uses ASP.NET Core Model Binding and Server-side Validation.
    /// </summary>
    [HttpPost("CreateRule")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRule(AutomationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Re-populate ancillary collections for the view
            model.ExistingRules = await _context.AutomationRules.OrderByDescending(r => r.RuleId).ToListAsync();
            model.Actuators = await _context.Actuators.Include(a => a.Zone).ToListAsync();
            model.SoilMetrics = new SoilHealthMetrics();
            return View("Index", model);
        }

        var rule = new AutomationRule
        {
            ConditionVariable = model.NewRule.ConditionVariable,
            Operator = model.NewRule.Operator,
            ThresholdValue = model.NewRule.ThresholdValue,
            ActionTarget = model.NewRule.ActionTarget,
            IsActive = model.NewRule.IsActive,
            Description = string.IsNullOrWhiteSpace(model.NewRule.Description)
                ? $"IF {model.NewRule.ConditionVariable} {model.NewRule.Operator} {model.NewRule.ThresholdValue} THEN {model.NewRule.ActionTarget}"
                : model.NewRule.Description,
            CreatedAt = IstClock.Now
        };

        _context.AutomationRules.Add(rule);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Automation Rule #{rule.RuleId} created successfully: IF {rule.ConditionVariable} {rule.Operator} {rule.ThresholdValue} THEN {rule.ActionTarget}";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Toggle rule activation status.
    /// </summary>
    [HttpPost("ToggleRule")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRule(int ruleId)
    {
        var rule = await _context.AutomationRules.FindAsync(ruleId);
        if (rule != null)
        {
            rule.IsActive = !rule.IsActive;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Rule #{rule.RuleId} is now {(rule.IsActive ? "ACTIVE" : "DISABLED")}.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Delete an automation rule.
    /// </summary>
    [HttpPost("DeleteRule")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRule(int ruleId)
    {
        var rule = await _context.AutomationRules.FindAsync(ruleId);
        if (rule != null)
        {
            _context.AutomationRules.Remove(rule);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Rule #{ruleId} removed from farm logic.";
        }

        return RedirectToAction(nameof(Index));
    }
}
