using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgriSyncIndia.Models;

/// <summary>
/// Physical or automated climate control device (Exhaust Fans, Misting Pump, Drip Solenoid Valves).
/// </summary>
public class Actuator
{
    [Key]
    public int ActuatorId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [ForeignKey("Zone")]
    public int ZoneId { get; set; }

    public bool IsAutoMode { get; set; } = true;

    [Required]
    [StringLength(20)]
    public string CurrentStatus { get; set; } = "OFF"; // "ON" or "OFF"

    public DateTime LastTriggered { get; set; } = DateTime.UtcNow.AddHours(5.5);

    [StringLength(50)]
    public string DeviceType { get; set; } = "Ventilation"; // Ventilation, Misting, Irrigation

    public double PowerRatingKw { get; set; } = 1.5;

    // Navigation property
    public virtual Zone? Zone { get; set; }
}
