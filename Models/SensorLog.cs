using AgriSyncIndia.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgriSyncIndia.Models;

/// <summary>
/// Telemetry log entry for an agricultural zone.
/// </summary>
public class SensorLog
{
    [Key]
    public int LogId { get; set; }

    [Required]
    [ForeignKey("Zone")]
    public int ZoneId { get; set; }

    public double Temperature { get; set; }

    public double Humidity { get; set; }

    public double VPD { get; set; }

    public double SoilMoisture { get; set; }

    public double SolarRadiation { get; set; }

    public double SoilTemperature { get; set; }

    public double WindSpeed { get; set; }

    public DateTime TimestampIST { get; set; } = IstClock.Now;

    // Navigation property
    public virtual Zone? Zone { get; set; }
}
