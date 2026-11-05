using System.ComponentModel.DataAnnotations;
using AgriSyncIndia.Models;

namespace AgriSyncIndia.ViewModels;

public class CreateFarmClusterViewModel
{
    [Required(ErrorMessage = "Farm cluster name is required.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "Cluster name must be between 3 and 120 characters.")]
    [Display(Name = "Farm Cluster Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Region / State is required.")]
    [StringLength(100, ErrorMessage = "Region cannot exceed 100 characters.")]
    [Display(Name = "Geographic Region / State")]
    public string Region { get; set; } = "Punjab Plains";

    [Required(ErrorMessage = "Latitude is required.")]
    [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90.0 and 90.0.")]
    [Display(Name = "GPS Latitude (°N)")]
    public double Latitude { get; set; } = 30.9010;

    [Required(ErrorMessage = "Longitude is required.")]
    [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180.0 and 180.0.")]
    [Display(Name = "GPS Longitude (°E)")]
    public double Longitude { get; set; } = 75.8573;

    [Required(ErrorMessage = "Acreage is required.")]
    [Range(0.5, 50000.0, ErrorMessage = "Total acreage must be between 0.5 and 50,000 acres.")]
    [Display(Name = "Cultivated Land Area (Acres)")]
    public double TotalAcreage { get; set; } = 50.0;

    [Required(ErrorMessage = "Primary crop varieties are required.")]
    [StringLength(200, ErrorMessage = "Crops string cannot exceed 200 characters.")]
    [Display(Name = "Primary Crop Varieties")]
    public string PrimaryCrops { get; set; } = "Pusa Basmati 1121, HD-2967 Wheat";

    [StringLength(300, ErrorMessage = "Description cannot exceed 300 characters.")]
    [Display(Name = "Agro-Ecological Description")]
    public string Description { get; set; } = "Precision agricultural cluster connected to regional Agromet telemetry network.";

    public string? ReturnUrl { get; set; }
}

public class FarmClusterListViewModel
{
    public List<FarmCluster> Clusters { get; set; } = new();
    public CreateFarmClusterViewModel NewCluster { get; set; } = new();
}
