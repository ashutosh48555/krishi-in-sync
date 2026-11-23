using AgriSyncIndia.Models;

namespace AgriSyncIndia.Services;

public interface IOpenMeteoService
{
    Task<OpenMeteoResponse?> FetchLiveWeatherAsync(double latitude = 28.6139, double longitude = 77.2090);
    
    Task<AmbientReading?> GetAmbientAsync();

    double CalculateVPD(double temperatureC, double relativeHumidityPercent);
    
    (string BadgeText, string BadgeCssClass, string Description) GetVpdStatus(double vpdKpa);
    
    Task<List<SensorLog>> IngestAndSyncTelemetryAsync();
    
    Task EvaluateAutomationRulesAsync(List<SensorLog> latestLogs);

    Task ToggleActuatorAsync(int actuatorId);
}

/// <summary>Ambient weather. IsStale means the stored last reading was used because Open-Meteo was unreachable.</summary>
public record AmbientReading(double Temp, double Humidity, double WindSpeed, double? Pressure, DateTime TimestampIst, bool IsStale);
