using System.ComponentModel.DataAnnotations;

namespace AgriSyncIndia.Models;

/// <summary>
/// Configurable IF-THEN automation rule for microclimate and soil management.
/// Example: IF RH > 75% THEN Turn ON Exhaust Fans
/// </summary>
public class AutomationRule
{
    [Key]
    public int RuleId { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Condition Variable")]
    public string ConditionVariable { get; set; } = "Relative Humidity"; // Relative Humidity, Temperature, VPD, Soil Moisture

    [Required]
    [StringLength(10)]
    public string Operator { get; set; } = ">"; // >, <, >=, <=, ==

    [Required]
    [Display(Name = "Threshold Value")]
    public double ThresholdValue { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Action Target")]
    public string ActionTarget { get; set; } = "Turn ON Exhaust Fans";

    [Display(Name = "Active Status")]
    public bool IsActive { get; set; } = true;

    [StringLength(200)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(5.5);
}
