using AgriSyncIndia.Services;
using System.ComponentModel.DataAnnotations;
using AgriSyncIndia.Models;

namespace AgriSyncIndia.ViewModels;

public class AnalyticsViewModel
{
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = IstClock.Now.AddDays(-1);

    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = IstClock.Now;

    [Display(Name = "Agricultural Zone")]
    public int? SelectedZoneId { get; set; }

    public List<Zone> AvailableZones { get; set; } = new();

    // Summary Statistics
    public double AvgTemp { get; set; }
    public double MinTemp { get; set; }
    public double MaxTemp { get; set; }
    public double AvgHumidity { get; set; }
    public double AvgVPD { get; set; }
    public double AvgSoilMoisture { get; set; }

    // Chart Data for Multi-trend Chart.js
    public List<string> ChartTimestamps { get; set; } = new();
    public List<double> ChartTemperatures { get; set; } = new();
    public List<double> ChartHumidities { get; set; } = new();
    public List<double> ChartVPDs { get; set; } = new();
    public List<double> ChartSoilMoistures { get; set; } = new();

    // Tabular Records
    public List<SensorLog> HistoricalRecords { get; set; } = new();
}
