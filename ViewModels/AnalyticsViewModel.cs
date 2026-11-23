namespace AgriSyncIndia.ViewModels;

/// <summary>Min, average and maximum of one measure over the selected range (from stored SensorLogs).</summary>
public record Stat(double Min, double Avg, double Max);

public record ZoneOption(int Id, string Name);

/// <summary>History page: range (24h or 7d), optional zone, summary cards and hourly chart data.</summary>
public class HistoryViewModel
{
    public string CurrentLanguage { get; set; } = "en";
    public string Range { get; set; } = "24h";
    public int? SelectedZoneId { get; set; }
    public List<ZoneOption> Zones { get; set; } = new();
    public int SampleCount { get; set; }
    public Stat? Temp { get; set; }
    public Stat? Humidity { get; set; }
    public Stat? Vpd { get; set; }
    public ChartDataViewModel Chart { get; set; } = new();
    /// <summary>Zone key when one zone is chosen, otherwise "all" (every zone drawn in its own series colour).</summary>
    public string Selected { get; set; } = "all";
}
