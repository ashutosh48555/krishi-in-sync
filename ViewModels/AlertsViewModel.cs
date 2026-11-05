using AgriSyncIndia.Models;

namespace AgriSyncIndia.ViewModels;

public class AlertsViewModel
{
    public string SelectedSeverity { get; set; } = "All"; // All, Critical, Warning, Info
    public List<ClimateAlert> Alerts { get; set; } = new();
    public List<RegionalCropAdvisory> Advisories { get; set; } = new();
    public int TotalUnacknowledged { get; set; }
    public int CriticalCount { get; set; }
    public int WarningCount { get; set; }
}

public class RegionalCropAdvisory
{
    public string CropName { get; set; } = string.Empty;
    public string StateRegion { get; set; } = string.Empty;
    public string GrowthStage { get; set; } = string.Empty;
    public string AdvisoryHeadline { get; set; } = string.Empty;
    public string DetailedInstructions { get; set; } = string.Empty;
    public string SourceInstitute { get; set; } = "ICAR-Indian Agricultural Research Institute / IMD";
    public DateTime IssuedDateIST { get; set; }
}
