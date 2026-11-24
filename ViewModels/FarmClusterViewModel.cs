using System.ComponentModel.DataAnnotations;
using AgriSyncIndia.Models;

namespace AgriSyncIndia.ViewModels;

/// <summary>Add-cluster form. <c>ErrorMessage</c> values are keys in <see cref="Services.Strings"/>.</summary>
public class CreateFarmClusterViewModel
{
    [Required(ErrorMessage = "val.cl.name")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "val.cl.name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "val.cl.region")]
    [StringLength(100, ErrorMessage = "val.cl.region")]
    public string Region { get; set; } = string.Empty;

    /// <summary>Both coordinates blank means the entity default (Delhi).</summary>
    [Range(-90.0, 90.0, ErrorMessage = "val.cl.lat")]
    public double? Latitude { get; set; }

    [Range(-180.0, 180.0, ErrorMessage = "val.cl.lon")]
    public double? Longitude { get; set; }

    [Required(ErrorMessage = "val.cl.acres")]
    [Range(0.5, 50000.0, ErrorMessage = "val.cl.acres")]
    public double? TotalAcreage { get; set; }

    [Required(ErrorMessage = "val.cl.crops")]
    [StringLength(200, ErrorMessage = "val.cl.crops")]
    public string PrimaryCrops { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class FarmClusterListViewModel
{
    public List<FarmCluster> Clusters { get; set; } = new();
    public int ZoneCount { get; set; }
    public string ActiveFarm { get; set; } = string.Empty;
}
