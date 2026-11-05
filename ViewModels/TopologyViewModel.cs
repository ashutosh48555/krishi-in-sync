namespace AgriSyncIndia.ViewModels;

public class TopologyViewModel
{
    public List<ThermalSectorInfo> SectorMatrix { get; set; } = new();
    public List<SensorNodeHealth> SensorNodes { get; set; } = new();
    public string FarmLayoutDescription { get; set; } = string.Empty;
    public double FarmAvgTemp { get; set; }
    public double MaxThermalDelta { get; set; }
}

public class ThermalSectorInfo
{
    public string SectorCode { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public string MicroclimateType { get; set; } = string.Empty;
    public double CurrentTemp { get; set; }
    public double AmbientDelta { get; set; }
    public double Humidity { get; set; }
    public double VPD { get; set; }
    public double SoilMoisture { get; set; }
    public string ThermalCategory { get; set; } = "Normal"; // Elevated, Baseline, Cooled
    public string ColorBadge { get; set; } = "border-success";
}

public class SensorNodeHealth
{
    public string NodeId { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public string SensorType { get; set; } = string.Empty;
    public int BatteryPercent { get; set; }
    public int SignalRssi { get; set; } // -55 dBm etc.
    public string TransmissionInterval { get; set; } = "15 min";
    public DateTime LastHeartbeatIST { get; set; }
    public DateTime LastCalibrationDate { get; set; }
    public string HealthStatus { get; set; } = "Healthy"; // Healthy, Low Battery, Warning
}
