using System.ComponentModel.DataAnnotations;

namespace AgriSyncIndia.Models;

/// <summary>
/// Represents a discrete agricultural zone on the smart farm.
/// </summary>
public class Zone
{
    [Key]
    public int ZoneId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string CropType { get; set; } = string.Empty;

    public double TargetTemp { get; set; }

    public double TargetHumidity { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Optimal"; // Optimal, Caution, Critical

    [StringLength(250)]
    public string Description { get; set; } = string.Empty;

    public double AreaSqMeters { get; set; } = 1000;

    [StringLength(50)]
    public string IconClass { get; set; } = "bi-flower1";

    // Navigation properties
    public virtual ICollection<SensorLog> SensorLogs { get; set; } = new List<SensorLog>();
    public virtual ICollection<Actuator> Actuators { get; set; } = new List<Actuator>();
    public virtual ICollection<ClimateAlert> Alerts { get; set; } = new List<ClimateAlert>();
}
