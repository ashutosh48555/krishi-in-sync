using AgriSyncIndia.Models;
using AgriSyncIndia.Services;

namespace AgriSyncIndia.ViewModels;

public class DashboardViewModel
{
    // Header Bar Selectors
    public string SelectedFarm { get; set; } = "IARI Pusa Smart Farm - New Delhi";
    public List<string> AvailableFarms { get; set; } = new()
    {
        "IARI Pusa Smart Farm - New Delhi",
        "PAU Ludhiana Smart Agro Block",
        "Karnal Agri-Tech Research Complex",
        "Nagpur Citrus & Precision Cluster"
    };

    public string SelectedCropPreset { get; set; } = "Pusa Basmati 1121";
    public List<string> CropPresets { get; set; } = new()
    {
        "Pusa Basmati 1121 (Aromatic Paddy)",
        "Sharbati Premium Wheat (Triticum aestivum)",
        "Kufri Jyoti Potato (Solanum tuberosum)",
        "Bt Hybrid Cotton (Gossypium hirsutum)"
    };

    public string CurrentLanguage { get; set; } = "en"; // en, hi, pa
    public string LiveIstTimestamp { get; set; } = string.Empty;
    public string WeatherBannerText { get; set; } = string.Empty;
    public double? AmbientTemp { get; set; }
    public double? AmbientHumidity { get; set; }
    public double? AmbientWindSpeed { get; set; }
    public double? SurfacePressure { get; set; }

    /// <summary>True when Open-Meteo was unreachable and the last stored reading is shown.</summary>
    public bool AmbientIsStale { get; set; }
    public string AmbientAsOf { get; set; } = string.Empty;

    // Plain-language summary built from zone statuses (ClimateRules)
    public string Headline { get; set; } = string.Empty;
    public string Advice { get; set; } = string.Empty;
    public string AdviceReason { get; set; } = string.Empty;

    // 4 Primary Metric Cards (ambient, from Open-Meteo)
    public double? PrimaryTemp { get; set; }
    public double? PrimaryHumidity { get; set; }
    public double? PrimaryVPD { get; set; }
    public ZoneCardViewModel? Polyhouse => ZoneCards.FirstOrDefault(z =>
        z.Name.Contains("Polyhouse", StringComparison.OrdinalIgnoreCase));
    public string VpdBadgeText { get; set; } = "Optimal Transpiration";
    public string VpdBadgeCssClass { get; set; } = "bg-success";
    public string VpdDescription { get; set; } = string.Empty;
    public double SolarRadiation { get; set; }

    // Microclimate Zone Cards
    public List<ZoneCardViewModel> ZoneCards { get; set; } = new();

    // Actuators Overrides
    public List<ActuatorViewModel> Actuators { get; set; } = new();

    // 24-Hour Diurnal Telemetry Chart Data for Chart.js
    public ChartDataViewModel ChartData { get; set; } = new();

    // Alert notification banner count
    public int ActiveAlertsCount { get; set; }
}

public class ZoneCardViewModel
{
    public int ZoneId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CropType { get; set; } = string.Empty;
    public string Status { get; set; } = "Optimal"; // Optimal, Caution, NoReading (display only)
    public ZoneStatus State { get; set; } = ZoneStatus.NoReading;
    public bool HasReading { get; set; }
    public double? Delta { get; set; }              // CurrentTemp - TargetTemp
    public VpdBand? VpdBand { get; set; }
    public string VpdBandText { get; set; } = string.Empty;
    public string IconClass { get; set; } = "bi-flower1";
    public double CurrentTemp { get; set; }
    public double TargetTemp { get; set; }
    public double CurrentHumidity { get; set; }
    public double TargetHumidity { get; set; }
    public double CurrentVPD { get; set; }
    public double CurrentSoilMoisture { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ActuatorViewModel
{
    public int ActuatorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public bool IsAutoMode { get; set; }
    public string CurrentStatus { get; set; } = "OFF"; // "ON" or "OFF"
    public string DeviceType { get; set; } = "Ventilation";
    public double PowerRatingKw { get; set; }
    public DateTime LastTriggered { get; set; }
}

public class ChartDataViewModel
{
    public List<string> Labels { get; set; } = new(); // "04:00 IST", "05:00 IST"
    public List<double> PolyhouseTemp { get; set; } = new();
    public List<double> OpenFieldTemp { get; set; } = new();
    public List<double> DripSectorTemp { get; set; } = new();
    public List<double> PolyhouseHumidity { get; set; } = new();
    public List<double> PolyhouseVPD { get; set; } = new();
}
