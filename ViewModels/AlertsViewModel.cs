namespace AgriSyncIndia.ViewModels;

public class AlertsViewModel
{
    public string CurrentLanguage { get; set; } = "en";
    public string Filter { get; set; } = "All"; // All, Critical, Warning, Resolved
    public bool CanAcknowledge { get; set; }
    public List<AlertRow> Alerts { get; set; } = new();
    /// <summary>Chip counts keyed by filter name. Critical and Warning count open alerts only.</summary>
    public Dictionary<string, int> Counts { get; set; } = new();
}

public class AlertRow
{
    public int Id { get; set; }
    public string Severity { get; set; } = "Info";
    public string Title { get; set; } = string.Empty;
    public string Advice { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public bool IsAcknowledged { get; set; }
}
