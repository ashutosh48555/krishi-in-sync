using System.Globalization;
using AgriSyncIndia.Data;
using AgriSyncIndia.Models;
using AgriSyncIndia.Services;
using AgriSyncIndia.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Controllers;

/// <summary>Chip filters: Critical and Warning show open alerts of that severity, Resolved shows acknowledged ones, All shows everything.</summary>
public static class AlertFilter
{
    public static readonly string[] Names = { "All", "Critical", "Warning", "Resolved" };

    public static string Normalize(string? f) => Names.FirstOrDefault(n => n.Equals(f, StringComparison.OrdinalIgnoreCase)) ?? "All";

    public static IEnumerable<ClimateAlert> Apply(IEnumerable<ClimateAlert> alerts, string? filter) => Normalize(filter) switch
    {
        "All" => alerts,
        "Resolved" => alerts.Where(a => a.IsAcknowledged),
        var sev => alerts.Where(a => !a.IsAcknowledged && a.Severity == sev)
    };
}

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

    /// <summary>Alerts (/Alerts). Open alerts first, newest first.</summary>
    [HttpGet]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? filter)
    {
        var lang = HttpContext.Lang();
        var f = AlertFilter.Normalize(filter);
        var all = await _context.ClimateAlerts.Include(a => a.Zone).AsNoTracking()
            .OrderBy(a => a.IsAcknowledged).ThenByDescending(a => a.TimestampIST).ToListAsync();

        string ZoneName(Zone? z)
        {
            if (z == null) return string.Empty;
            var key = DashboardViewModelBuilder.ZoneKey(z.Name);
            return key.Length > 0 ? Localizer.T(lang, "zone." + key) : z.Name;
        }

        return View(new AlertsViewModel
        {
            CurrentLanguage = lang,
            Filter = f,
            CanAcknowledge = Permissions.IsOperator(User),
            Counts = AlertFilter.Names.ToDictionary(n => n, n => AlertFilter.Apply(all, n).Count()),
            Alerts = AlertFilter.Apply(all, f).Select(a => new AlertRow
            {
                Id = a.AlertId,
                Severity = a.Severity,
                Title = a.Title,
                Advice = string.IsNullOrWhiteSpace(a.AgronomistRecommendation) ? a.Message : a.AgronomistRecommendation,
                ZoneName = ZoneName(a.Zone),
                TimeText = a.TimestampIST.ToString("dd MMM, HH:mm", CultureInfo.InvariantCulture),
                IsAcknowledged = a.IsAcknowledged
            }).ToList()
        });
    }

    /// <summary>Acknowledge an alert. Operators only; anyone else gets the same 404 as a missing alert.</summary>
    [HttpPost("Acknowledge")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Acknowledge(int alertId, string? filter)
    {
        var alert = await _context.ClimateAlerts.FindAsync(alertId);
        if (alert == null || !Permissions.IsOperator(User)) return NotFound();
        alert.IsAcknowledged = true;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = HttpContext.T("al.acked", alert.Title);
        return RedirectToAction(nameof(Index), new { filter = AlertFilter.Normalize(filter) });
    }
}
