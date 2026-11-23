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

    /// <summary>
    /// GET: /FarmCluster/Index - View all registered farm clusters
    /// </summary>
    [HttpGet]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var clusters = await _context.FarmClusters
            .OrderByDescending(c => c.ClusterId)
            .ToListAsync();

        var model = new FarmClusterListViewModel
        {
            Clusters = clusters,
            NewCluster = new CreateFarmClusterViewModel()
        };

        return View(model);
    }

    /// <summary>
    /// GET: /FarmCluster/Create - Form to add a new farm cluster
    /// </summary>
    [HttpGet("Create")]
    public IActionResult Create(string? returnUrl)
    {
        return View(new CreateFarmClusterViewModel { ReturnUrl = returnUrl });
    }

    /// <summary>
    /// POST: /FarmCluster/Create - Adds a new farm cluster and saves to EF Core DB
    /// </summary>
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateFarmClusterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Check if cluster name already exists
        bool exists = await _context.FarmClusters.AnyAsync(c => c.Name.ToLower() == model.Name.Trim().ToLower());
        if (exists)
        {
            ModelState.AddModelError("Name", "A farm cluster with this name already exists in the system.");
            return View(model);
        }

        var cluster = new FarmCluster
        {
            Name = model.Name.Trim(),
            Region = model.Region.Trim(),
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            TotalAcreage = model.TotalAcreage,
            PrimaryCrops = model.PrimaryCrops.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) 
                ? $"Smart agricultural research block in {model.Region} cultivating {model.PrimaryCrops}."
                : model.Description.Trim(),
            Status = "Active",
            CreatedAt = IstClock.Now
        };

        _context.FarmClusters.Add(cluster);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added new farm cluster: {Name} in {Region}", cluster.Name, cluster.Region);
        TempData["SuccessMessage"] = $"Farm Cluster '{cluster.Name}' registered successfully! It is now live in your farm selector dropdown.";

        // If returnUrl specified and valid, redirect there with the new farm selected
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        // Redirect directly to Dashboard with the new farm cluster selected!
        return RedirectToAction("Dashboard", "Climate", new { farm = cluster.Name });
    }
}
