using System.ComponentModel.DataAnnotations;

namespace AgriSyncIndia.Models;

/// <summary>
/// Represents a smart agricultural farm cluster or agro-meteorological research complex.
/// </summary>
public class FarmCluster
{
    [Key]
    public int ClusterId { get; set; }

    [Required(ErrorMessage = "Farm cluster name is required.")]
    [StringLength(120, ErrorMessage = "Cluster name cannot exceed 120 characters.")]
    [Display(Name = "Cluster Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Region / State is required.")]
    [StringLength(100)]
    [Display(Name = "Region / State")]
    public string Region { get; set; } = "Indo-Gangetic Basin";

    [Display(Name = "Latitude (°N)")]
    [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double Latitude { get; set; } = 28.6139;

    [Display(Name = "Longitude (°E)")]
    [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double Longitude { get; set; } = 77.2090;

    [Display(Name = "Total Acreage (Acres)")]
    [Range(0.5, 50000.0, ErrorMessage = "Acreage must be at least 0.5 acres.")]
    public double TotalAcreage { get; set; } = 25.0;

    [Required(ErrorMessage = "Primary crop varieties are required.")]
    [StringLength(200)]
    [Display(Name = "Primary Crop Varieties")]
    public string PrimaryCrops { get; set; } = "Pusa Basmati 1121, Sharbati Wheat";

    [StringLength(300)]
    [Display(Name = "Cluster Description")]
    public string Description { get; set; } = string.Empty;

    [StringLength(30)]
    public string Status { get; set; } = "Active"; // Active, Planning, Maintenance

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(5.5);
}
