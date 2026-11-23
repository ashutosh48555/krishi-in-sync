namespace AgriSyncIndia.ViewModels;

/// <summary>Zones page: one card per zone from the DB. Nothing here is typed in; no reading means no numbers.</summary>
public class TopologyViewModel
{
    public string CurrentLanguage { get; set; } = "en";
    public bool CanToggle { get; set; }
    public List<TopoZone> Zones { get; set; } = new();
}

public class TopoZone
{
    public ZoneCardViewModel Card { get; set; } = new();
    public string CropType { get; set; } = string.Empty;
    public double AreaSqMeters { get; set; }
    public string LastReading { get; set; } = string.Empty; // IST "dd MMM, HH:mm" or empty
    public List<TopoActuator> Actuators { get; set; } = new();
}

public record TopoActuator(int Id, string Name, string DeviceType, bool On, bool Auto);
