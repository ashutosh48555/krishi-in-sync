using AgriSyncIndia.Models;

namespace AgriSyncIndia.Services;

public interface IOpenMeteoService
{
    Task<OpenMeteoResponse?> FetchLiveWeatherAsync(double latitude = 28.6139, double longitude = 77.2090);
    
    double CalculateVPD(double temperatureC, double relativeHumidityPercent);
    
    (string BadgeText, string BadgeCssClass, string Description) GetVpdStatus(double vpdKpa);
    
    Task<List<SensorLog>> IngestAndSyncTelemetryAsync();
    
    Task EvaluateAutomationRulesAsync(List<SensorLog> latestLogs);

    Task ToggleActuatorAsync(int actuatorId);
}
