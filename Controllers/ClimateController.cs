using Microsoft.AspNetCore.Authorization;
using AgriSyncIndia.Data;
using AgriSyncIndia.Services;
using AgriSyncIndia.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Controllers;

[Route("[controller]")]
public partial class ClimateController : Controller
{
    private readonly AgriSyncDbContext _context;
    private readonly IOpenMeteoService _weatherService;
    private readonly ILogger<ClimateController> _logger;

    public ClimateController(
        AgriSyncDbContext context,
        IOpenMeteoService weatherService,
        ILogger<ClimateController> logger)
    {
        _context = context;
        _weatherService = weatherService;
        _logger = logger;
    }

    /// <summary>Dashboard (/Climate/Dashboard). Home (/) uses the same builder.</summary>
    [HttpGet]
    [HttpGet("Dashboard")]
    public async Task<IActionResult> Dashboard([FromServices] DashboardViewModelBuilder builder, string? farm)
        => View(await builder.BuildAsync(HttpContext.Lang(), farm));

    /// <summary>
    /// Actuator manual toggle action.
    /// </summary>
    [HttpPost("ToggleActuator")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActuator(int actuatorId, string? returnUrl)
    {
        if (!Permissions.IsOperator(User) || !await _context.Actuators.AnyAsync(a => a.ActuatorId == actuatorId)) return NotFound();
        await _weatherService.ToggleActuatorAsync(actuatorId);
        TempData["SuccessMessage"] = $"Actuator #{actuatorId} status successfully toggled.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Dashboard));
    }

    /// <summary>
    /// Ingests live telemetry from Open-Meteo REST API.
    /// </summary>
    [HttpPost("RefreshTelemetry")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshTelemetry(string? returnUrl)
    {
        try
        {
            var logs = await _weatherService.IngestAndSyncTelemetryAsync();
            if (logs.Count > 0)
                TempData["SuccessMessage"] = $"Readings refreshed from Open-Meteo for {logs.Count} zones.";
            else
                TempData["ErrorMessage"] = "Could not reach Open-Meteo. Showing the last stored readings. Try again in a minute.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing telemetry");
            TempData["ErrorMessage"] = "Could not refresh readings. Showing the last stored readings. Try again in a minute.";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Dashboard));
    }
}
