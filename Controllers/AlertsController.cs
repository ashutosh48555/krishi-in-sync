using Microsoft.AspNetCore.Authorization;
using AgriSyncIndia.Services;
using AgriSyncIndia.Data;
using AgriSyncIndia.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Controllers;

[Route("[controller]")]
public class AlertsController : Controller
{
    private readonly AgriSyncDbContext _context;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(AgriSyncDbContext context, ILogger<AlertsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// View 5: Alerts & Advisory (/Alerts/Index or /Alerts)
    /// </summary>
    [HttpGet]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? severity)
    {
        var filter = string.IsNullOrEmpty(severity) ? "All" : severity;

        var query = _context.ClimateAlerts
            .Include(a => a.Zone)
            .AsNoTracking();

        if (!filter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(a => a.Severity == filter);
        }

        var alerts = await query
            .OrderBy(a => a.IsAcknowledged)
            .ThenByDescending(a => a.TimestampIST)
            .ToListAsync();

        int criticalCount = await _context.ClimateAlerts.CountAsync(a => a.Severity == "Critical" && !a.IsAcknowledged);
        int warningCount = await _context.ClimateAlerts.CountAsync(a => a.Severity == "Warning" && !a.IsAcknowledged);
        int unackCount = await _context.ClimateAlerts.CountAsync(a => !a.IsAcknowledged);

        var nowIst = IstClock.Now;

        // Indian Regional Agromet Advisories (ICAR / IMD bulletins)
        var advisories = new List<RegionalCropAdvisory>
        {
            new RegionalCropAdvisory
            {
                CropName = "Pusa Basmati 1121 & 1509 Paddy",
                StateRegion = "Punjab, Haryana & Western Uttar Pradesh",
                GrowthStage = "Seedling to Tillering Phase",
                AdvisoryHeadline = "Fungal Blast & Bacterial Blight Precautionary Window",
                DetailedInstructions = "High relative humidity (>75%) coupled with moderate nocturnal temperatures (18-22°C) creates high sporulation potential for Magnaporthe oryzae. Ensure nursery drainage channels are free of standing water. If leaf spindle lesions appear, spray Tricyclazole 75% WP @ 0.6g/L or Isoprothiolane 40% EC @ 1.5ml/L during morning hours.",
                SourceInstitute = "ICAR-Indian Agricultural Research Institute (IARI), New Delhi",
                IssuedDateIST = nowIst.AddDays(-1)
            },
            new RegionalCropAdvisory
            {
                CropName = "Sharbati & HD-2967 Wheat",
                StateRegion = "Indo-Gangetic Alluvial Belt",
                GrowthStage = "Crown Root Initiation (CRI) / Early Vegetative",
                AdvisoryHeadline = "Moisture Stress & Terminal Heat Mitigation",
                DetailedInstructions = "Monitor root zone soil moisture using AgriSync tensiometer telemetry. For light textured soils, initiate light irrigation 21-25 days after sowing. Avoid heavy flood irrigation if afternoon temperatures climb above 28°C to prevent soil compaction and root suffocation.",
                SourceInstitute = "ICAR-Indian Institute of Wheat and Barley Research (IIWBR), Karnal",
                IssuedDateIST = nowIst.AddDays(-2)
            },
            new RegionalCropAdvisory
            {
                CropName = "Kufri Jyoti & Chipsona Potato",
                StateRegion = "Jalandhar & Meerut Precision Potato Belt",
                GrowthStage = "Tuber Initiation & Bulking",
                AdvisoryHeadline = "Late Blight (Phytophthora infestans) Alert Matrix",
                DetailedInstructions = "Microclimate sensors indicating micro-drip canopy humidity above 85% for continuous 12-hour intervals trigger Late Blight advisory. Farmers are advised to conduct preventive spray with Mancozeb 75% WP @ 2.5g/L water before cloud cover intensifies.",
                SourceInstitute = "ICAR-Central Potato Research Institute (CPRI), Shimla",
                IssuedDateIST = nowIst.AddDays(-3)
            }
        };

        var viewModel = new AlertsViewModel
        {
            SelectedSeverity = filter,
            Alerts = alerts,
            Advisories = advisories,
            CriticalCount = criticalCount,
            WarningCount = warningCount,
            TotalUnacknowledged = unackCount
        };

        return View(viewModel);
    }

    /// <summary>
    /// Acknowledge an alert notification.
    /// </summary>
    [HttpPost("Acknowledge")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Acknowledge(int alertId, string? severity)
    {
        var alert = await _context.ClimateAlerts.FindAsync(alertId);
        if (alert == null || !Permissions.IsOperator(User)) return NotFound();
        alert.IsAcknowledged = true;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Alert '{alert.Title}' marked as acknowledged.";

        return RedirectToAction(nameof(Index), new { severity });
    }
}
