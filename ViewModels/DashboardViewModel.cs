using AgriSyncIndia.Services;

namespace AgriSyncIndia.ViewModels;

/// <summary>Everything Home and Climate/Dashboard show. Built by <see cref="DashboardViewModelBuilder"/>.</summary>
public class DashboardViewModel
{
    public string CurrentLanguage { get; set; } = "en"; // en, hi, pa
    public string SelectedFarm { get; set; } = string.Empty;
    public string SelectedRegion { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;   // short place name for the live chip
    public List<string> AvailableFarms { get; set; } = new();

    /// <summary>Latest stored zone reading, IST "HH:mm". Empty when no zone has a recent log.</summary>
    public string UpdatedAt { get; set; } = string.Empty;

    // Ambient (Open-Meteo, or the last stored reading when unreachable)
    public double? AmbientTemp { get; set; }
    public double? AmbientHumidity { get; set; }
    public double? AmbientVpd { get; set; }
    public bool AmbientIsStale { get; set; }
    public string AmbientAsOf { get; set; } = string.Empty;

    public string Headline { get; set; } = string.Empty;
    public string Advice { get; set; } = string.Empty;
    public string AdviceReason { get; set; } = string.Empty;

    public List<ZoneCardViewModel> ZoneCards { get; set; } = new();
    public ChartDataViewModel Chart { get; set; } = new();
    public List<AlertItemViewModel> Alerts { get; set; } = new();
    public int ActiveAlertsCount { get; set; }
}

public class ZoneCardViewModel
{
    public int ZoneId { get; set; }
    public string Key { get; set; } = string.Empty;         // poly, open, drip (3D id)
    public string Name { get; set; } = string.Empty;        // as stored
    public string DisplayName { get; set; } = string.Empty; // in the page language
    public ZoneStatus State { get; set; } = ZoneStatus.NoReading;
    public bool HasReading { get; set; }
    public double? Temp { get; set; }
    public double? Humidity { get; set; }
    public double? Vpd { get; set; }
    public double TargetTemp { get; set; }
    public double TargetHumidity { get; set; }
    public double? Delta { get; set; }                      // Temp - TargetTemp
    public VpdBand? VpdBand { get; set; }
    public string ActuatorName { get; set; } = string.Empty;
    public bool? ActuatorOn { get; set; }
}

public class ChartSeriesViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<double?> Temp { get; set; } = new();
    public List<double?> Vpd { get; set; } = new();
}

/// <summary>Hourly buckets, oldest first. A null means that zone had no log in that hour (never filled in).</summary>
public class ChartDataViewModel
{
    public List<string> Labels { get; set; } = new();
    public List<ChartSeriesViewModel> Series { get; set; } = new();
}

public class AlertItemViewModel
{
    public string Severity { get; set; } = "Info"; // Critical, Warning, Info
    public string Title { get; set; } = string.Empty;
    public string Advice { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
}
