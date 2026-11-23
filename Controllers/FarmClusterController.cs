using Microsoft.AspNetCore.Authorization;
using AgriSyncIndia.Services;
using AgriSyncIndia.Data;
using AgriSyncIndia.Models;
using AgriSyncIndia.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Controllers;

[Route("[controller]")]
public class FarmClusterController : Controller
{
    private readonly AgriSyncDbContext _context;
    private readonly ILogger<FarmClusterController> _logger;

    public FarmClusterController(AgriSyncDbContext context, ILogger<FarmClusterController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [HttpGet("Index")]
    public async Task<IActionResult> Index() => View(new FarmClusterListViewModel
    {
        Clusters = await _context.FarmClusters.AsNoTracking().OrderByDescending(c => c.ClusterId).ToListAsync(),
        ZoneCount = await _context.Zones.CountAsync()
    });

    [HttpGet("Create")]
    [Authorize]
    public IActionResult Create(string? returnUrl) => View(new CreateFarmClusterViewModel { ReturnUrl = returnUrl });

    [HttpPost("Create")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateFarmClusterViewModel model)
    {
        var name = model.Name?.Trim() ?? "";
        if (ModelState.IsValid && await _context.FarmClusters.AnyAsync(c => c.Name.ToLower() == name.ToLower()))
            ModelState.AddModelError(nameof(model.Name), "val.cl.dup");
        if (model.Latitude.HasValue != model.Longitude.HasValue)
            ModelState.AddModelError(model.Latitude.HasValue ? nameof(model.Longitude) : nameof(model.Latitude), "val.cl.both");
        if (!ModelState.IsValid) return View(model);

        var defaults = new FarmCluster();
        var cluster = new FarmCluster
        {
            Name = name,
            Region = model.Region.Trim(),
            Latitude = model.Latitude ?? defaults.Latitude,
            Longitude = model.Longitude ?? defaults.Longitude,
            TotalAcreage = model.TotalAcreage ?? defaults.TotalAcreage,
            PrimaryCrops = model.PrimaryCrops.Trim(),
            Status = "Active",
            CreatedAt = IstClock.Now
        };

        _context.FarmClusters.Add(cluster);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added new farm cluster: {Name} in {Region}", cluster.Name, cluster.Region);
        TempData["SuccessMessage"] = string.Format(Localizer.T(HttpContext.Lang(), "cl.t.added"), cluster.Name);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);
        return RedirectToAction("Dashboard", "Climate", new { farm = cluster.Name });
    }
}
