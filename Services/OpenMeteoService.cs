using System.Text.Json;
using AgriSyncIndia.Data;
using AgriSyncIndia.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Services;

public class OpenMeteoService : IOpenMeteoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OpenMeteoService> _logger;

    public OpenMeteoService(
        IHttpClientFactory httpClientFactory,
        IServiceProvider serviceProvider,
        ILogger<OpenMeteoService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Fetches live weather telemetry from Open-Meteo REST API.
    /// Default coordinates: Delhi / Indo-Gangetic Plains (Lat: 28.6139, Long: 77.2090).
    /// </summary>
    public async Task<OpenMeteoResponse?> FetchLiveWeatherAsync(double latitude = 28.6139, double longitude = 77.2090)
    {
        var client = _httpClientFactory.CreateClient("OpenMeteoClient");
        string url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}" +
                     "&current=temperature_2m,relative_humidity_2m,surface_pressure,wind_speed_10m,soil_temperature_0cm,soil_moisture_0_to_1cm" +
                     "&hourly=temperature_2m,relative_humidity_2m" +
                     "&timezone=Asia%2FKolkata";

        try
        {
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<OpenMeteoResponse>(json);
                return data;
            }
            _logger.LogWarning("Open-Meteo API returned non-success code: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch live weather from Open-Meteo API. Applying resilient fallback.");
        }

        // Return simulated baseline if network call fails
        return GenerateResilientFallback();
    }

    /// <summary>
    /// Computes Vapor Pressure Deficit (VPD in kPa) using C# math functions:
    /// VP_sat = 0.61078 * exp((17.27 * T) / (T + 237.3))
    /// VP_act = VP_sat * (RH / 100)
    /// VPD = VP_sat - VP_act
    /// </summary>
    public double CalculateVPD(double temperatureC, double relativeHumidityPercent)
    {
        if (temperatureC < -50 || temperatureC > 60 || relativeHumidityPercent <= 0)
            return 0.85;

        // Saturated Vapor Pressure in kPa
        double vpSat = 0.61078 * Math.Exp((17.27 * temperatureC) / (temperatureC + 237.3));

        // Actual Vapor Pressure in kPa
        double vpAct = vpSat * (relativeHumidityPercent / 100.0);

        // Vapor Pressure Deficit (VPD)
        double vpd = vpSat - vpAct;

        return Math.Round(Math.Max(0.01, vpd), 2);
    }

    /// <summary>
    /// Categorizes VPD into agronomic transpiration levels.
    /// </summary>
    public (string BadgeText, string BadgeCssClass, string Description) GetVpdStatus(double vpdKpa)
    {
        return vpdKpa switch
        {
            < 0.40 => ("Low VPD - Fungal Risk", "bg-warning text-dark", "High air saturation; risk of fungal pathogens (blast/blight) and poor calcium uptake."),
            >= 0.40 and < 0.80 => ("Vegetative Stage Range", "bg-info text-dark", "Mild transpiration; optimal for early root formation and young seedlings."),
            >= 0.80 and <= 1.25 => ("Optimal Transpiration", "bg-success", "Ideal stomatal conductance; maximum photosynthetic carbon fixation for Indian crops."),
            > 1.25 and <= 1.60 => ("Moderate Water Demand", "bg-primary", "Elevated transpiration; root zone moisture must be monitored."),
            _ => ("Severe Stress - Stomatal Closure", "bg-danger", "High evaporative demand causing stomatal closure, wilting risk, and heat shock.")
        };
    }

    /// <summary>
    /// Ingests live telemetry, applies C# microclimate offsets for 3 farm zones,
    /// logs readings to database, and triggers automation rules.
    /// </summary>
    public async Task<List<SensorLog>> IngestAndSyncTelemetryAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AgriSyncDbContext>();

        var weatherData = await FetchLiveWeatherAsync();
        var current = weatherData?.Current ?? GenerateResilientFallback().Current!;

        var nowIst = DateTime.UtcNow.AddHours(5.5);
        var createdLogs = new List<SensorLog>();

        var zones = await context.Zones.ToListAsync();
        if (!zones.Any())
            return createdLogs;

        foreach (var zone in zones)
        {
            double temp, rh, soilMoist, solarRad, soilTemp, wind;

            switch (zone.Name.ToLowerInvariant())
            {
                case string n when n.Contains("polyhouse") || n.Contains("nursery"):
                    // Polyhouse Nursery: Enclosed protected microclimate (+3.2°C, +8.5% RH, low wind, controlled soil)
                    temp = Math.Round(current.Temperature2m + 3.2, 1);
                    rh = Math.Round(Math.Min(95.0, current.RelativeHumidity2m + 8.5), 1);
                    soilMoist = Math.Round(Math.Min(85.0, (current.SoilMoisture0To1cm * 100.0) + 18.0), 1);
                    solarRad = 450.0; // Filtered UV diffuse radiation
                    soilTemp = Math.Round(current.SoilTemperature0cm + 1.8, 1);
                    wind = 0.5; // Fan circulation speed
                    break;

                case string n when n.Contains("drip") || n.Contains("sector"):
                    // Drip Sector: Evaporative ground cooling (-1.2°C, +4.0% RH, elevated root zone moisture)
                    temp = Math.Round(current.Temperature2m - 1.2, 1);
                    rh = Math.Round(Math.Min(92.0, current.RelativeHumidity2m + 4.0), 1);
                    soilMoist = Math.Round(Math.Min(90.0, (current.SoilMoisture0To1cm * 100.0) + 26.0), 1);
                    solarRad = 680.0;
                    soilTemp = Math.Round(current.SoilTemperature0cm - 1.5, 1);
                    wind = Math.Round(current.WindSpeed10m * 0.9, 1);
                    break;

                default:
                    // Open Field: Baseline ambient weather
                    temp = Math.Round(current.Temperature2m, 1);
                    rh = Math.Round(current.RelativeHumidity2m, 1);
                    soilMoist = Math.Round(Math.Min(100.0, current.SoilMoisture0To1cm * 100.0), 1);
                    solarRad = 740.0;
                    soilTemp = Math.Round(current.SoilTemperature0cm, 1);
                    wind = Math.Round(current.WindSpeed10m, 1);
                    break;
            }

            double vpd = CalculateVPD(temp, rh);

            var log = new SensorLog
            {
                ZoneId = zone.ZoneId,
                Temperature = temp,
                Humidity = rh,
                VPD = vpd,
                SoilMoisture = soilMoist,
                SolarRadiation = solarRad,
                SoilTemperature = soilTemp,
                WindSpeed = wind,
                TimestampIST = nowIst
            };

            context.SensorLogs.Add(log);
            createdLogs.Add(log);

            // Update zone status according to targets
            double tempDiff = Math.Abs(temp - zone.TargetTemp);
            double rhDiff = Math.Abs(rh - zone.TargetHumidity);

            if (tempDiff <= 3.0 && rhDiff <= 12.0)
                zone.Status = "Optimal";
            else if (tempDiff <= 6.0 && rhDiff <= 25.0)
                zone.Status = "Caution";
            else
                zone.Status = "Critical";
        }

        await context.SaveChangesAsync();

        // Evaluate automation rules
        await EvaluateAutomationRulesAsync(createdLogs);

        return createdLogs;
    }

    /// <summary>
    /// Evaluates automation rules against current sensor telemetry and updates actuators.
    /// </summary>
    public async Task EvaluateAutomationRulesAsync(List<SensorLog> latestLogs)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AgriSyncDbContext>();

        var rules = await context.AutomationRules.Where(r => r.IsActive).ToListAsync();
        var actuators = await context.Actuators.Include(a => a.Zone).ToListAsync();
        var nowIst = DateTime.UtcNow.AddHours(5.5);

        foreach (var rule in rules)
        {
            foreach (var log in latestLogs)
            {
                double measuredValue = rule.ConditionVariable.ToLowerInvariant() switch
                {
                    "relative humidity" or "humidity" or "rh" => log.Humidity,
                    "temperature" or "temp" => log.Temperature,
                    "vpd" => log.VPD,
                    "soil moisture" => log.SoilMoisture,
                    _ => log.Temperature
                };

                bool conditionMet = rule.Operator switch
                {
                    ">" => measuredValue > rule.ThresholdValue,
                    "<" => measuredValue < rule.ThresholdValue,
                    ">=" => measuredValue >= rule.ThresholdValue,
                    "<=" => measuredValue <= rule.ThresholdValue,
                    "==" => Math.Abs(measuredValue - rule.ThresholdValue) < 0.1,
                    _ => false
                };

                if (conditionMet)
                {
                    // Find target actuators matching action
                    var targetActuators = actuators.Where(a =>
                        (rule.ActionTarget.Contains("Exhaust", StringComparison.OrdinalIgnoreCase) && a.Name.Contains("Exhaust", StringComparison.OrdinalIgnoreCase)) ||
                        (rule.ActionTarget.Contains("Misting", StringComparison.OrdinalIgnoreCase) && a.Name.Contains("Misting", StringComparison.OrdinalIgnoreCase)) ||
                        (rule.ActionTarget.Contains("Drip", StringComparison.OrdinalIgnoreCase) && a.Name.Contains("Drip", StringComparison.OrdinalIgnoreCase))
                    ).ToList();

                    bool shouldTurnOn = rule.ActionTarget.Contains("Turn ON", StringComparison.OrdinalIgnoreCase);

                    foreach (var actuator in targetActuators)
                    {
                        if (actuator.IsAutoMode)
                        {
                            actuator.CurrentStatus = shouldTurnOn ? "ON" : "OFF";
                            actuator.LastTriggered = nowIst;
                        }
                    }

                    // Check if high humidity fungal alert is needed
                    if (rule.ConditionVariable.Contains("Humidity", StringComparison.OrdinalIgnoreCase) && measuredValue > 75.0)
                    {
                        bool exists = await context.ClimateAlerts.AnyAsync(a => 
                            a.Title.Contains("High Humidity Fungal Risk") && 
                            !a.IsAcknowledged && 
                            a.TimestampIST >= nowIst.AddHours(-3));

                        if (!exists)
                        {
                            context.ClimateAlerts.Add(new ClimateAlert
                            {
                                ZoneId = log.ZoneId,
                                Severity = "Critical",
                                Title = "High Humidity Fungal Risk Warning",
                                Message = $"Relative humidity reached {measuredValue}% in Zone #{log.ZoneId}. Microclimate is conducive to fungal spore germination (Rice Blast / Powdery Mildew).",
                                AgronomistRecommendation = "Engage exhaust ventilation fans immediately. Suspend foliar spray until RH drops below 70%.",
                                TimestampIST = nowIst,
                                IsAcknowledged = false
                            });
                        }
                    }
                }
            }
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Manually toggles an actuator between ON and OFF states.
    /// </summary>
    public async Task ToggleActuatorAsync(int actuatorId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AgriSyncDbContext>();

        var actuator = await context.Actuators.FindAsync(actuatorId);
        if (actuator != null)
        {
            actuator.CurrentStatus = actuator.CurrentStatus == "ON" ? "OFF" : "ON";
            actuator.LastTriggered = DateTime.UtcNow.AddHours(5.5);
            await context.SaveChangesAsync();
        }
    }

    private OpenMeteoResponse GenerateResilientFallback()
    {
        var nowIst = DateTime.UtcNow.AddHours(5.5);
        int hour = nowIst.Hour;

        // Realistic Indian Diurnal Curve (cooler night 18-20°C, warmer afternoon 28-32°C)
        double diurnalFactor = Math.Sin((hour - 8) * Math.PI / 12.0);
        double temp = Math.Round(24.0 + (diurnalFactor * 6.5), 1);
        double rh = Math.Round(72.0 - (diurnalFactor * 22.0), 1);

        var hourlyTimes = new List<string>();
        var hourlyTemps = new List<double>();
        var hourlyRhs = new List<double>();

        for (int i = 23; i >= 0; i--)
        {
            var hTime = nowIst.AddHours(-i);
            int hHour = hTime.Hour;
            double hFactor = Math.Sin((hHour - 8) * Math.PI / 12.0);
            double hTemp = Math.Round(24.0 + (hFactor * 6.5), 1);
            double hRh = Math.Round(72.0 - (hFactor * 22.0), 1);

            hourlyTimes.Add(hTime.ToString("yyyy-MM-ddTHH:00"));
            hourlyTemps.Add(hTemp);
            hourlyRhs.Add(hRh);
        }

        return new OpenMeteoResponse
        {
            Latitude = 28.6139,
            Longitude = 77.2090,
            Timezone = "Asia/Kolkata",
            Current = new OpenMeteoCurrent
            {
                Time = nowIst.ToString("yyyy-MM-ddTHH:mm"),
                Temperature2m = temp,
                RelativeHumidity2m = rh,
                SurfacePressure = 992.4,
                WindSpeed10m = 5.2,
                SoilTemperature0cm = Math.Round(temp - 2.0, 1),
                SoilMoisture0To1cm = 0.34
            },
            Hourly = new OpenMeteoHourly
            {
                Time = hourlyTimes,
                Temperature2m = hourlyTemps,
                RelativeHumidity2m = hourlyRhs
            }
        };
    }
}
