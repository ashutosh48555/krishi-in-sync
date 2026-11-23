using AgriSyncIndia.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgriSyncIndia.Models;

/// <summary>
/// Climate and agronomical alert notification for farmers and managers.
/// </summary>
public class ClimateAlert
{
    [Key]
    public int AlertId { get; set; }

    [ForeignKey("Zone")]
    public int? ZoneId { get; set; }

    [Required]
    [StringLength(20)]
    public string Severity { get; set; } = "Warning"; // Critical, Warning, Info

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    [StringLength(250)]
    public string AgronomistRecommendation { get; set; } = string.Empty;

    public DateTime TimestampIST { get; set; } = IstClock.Now;

    public bool IsAcknowledged { get; set; } = false;

    // Navigation property
    public virtual Zone? Zone { get; set; }
}
