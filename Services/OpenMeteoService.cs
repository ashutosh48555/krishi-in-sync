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

        // 5 s per attempt, one retry. Returns null when both attempts fail; never throws.
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await client.GetAsync(url, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cts.Token);
                    var data = JsonSerializer.Deserialize<OpenMeteoResponse>(json);
                    if (data?.Current != null) return data;
                }
                else
                {
                    _logger.LogWarning("Open-Meteo returned {StatusCode} (attempt {Attempt})", response.StatusCode, attempt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Open-Meteo request failed (attempt {Attempt})", attempt);
            }
        }

        return null;
    }

    /// <summary>
    /// Ambient reading for the dashboard: live Open-Meteo when reachable, else the newest stored
    /// Open Field log flagged as stale. Null only when neither exists. Never throws.
    /// </summary>
    public async Task<AmbientReading?> GetAmbientAsync()
    {
        var live = (await FetchLiveWeatherAsync())?.Current;
        if (live != null)
            return new AmbientReading(live.Temperature2m, live.RelativeHumidity2m, live.WindSpeed10m,
                live.SurfacePressure, IstClock.Now, false);

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AgriSyncDbContext>();
        var last = await context.SensorLogs
            .Where(l => l.Zone!.Name == "Open Field")
            .OrderByDescending(l => l.TimestampIST)
            .FirstOrDefaultAsync();
        return last == null ? null
            : new AmbientReading(last.Temperature, last.Humidity, last.WindSpeed, null, last.TimestampIST, true);
    }

    /// <summary>
    /// Computes Vapor Pressure Deficit (VPD in kPa) using C# math functions:
    /// Delegates to ClimateRules.Vpd (single formula), rounded to 2 decimals.
    /// </summary>
    public double CalculateVPD(double temperatureC, double relativeHumidityPercent)
    {
        if (temperatureC < -50 || temperatureC > 60 || relativeHumidityPercent <= 0)
            return 0.85;

        double vpd = ClimateRules.Vpd(temperatureC, relativeHumidityPercent);

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
        var current = weatherData?.Current;
        if (current == null)
            return new List<SensorLog>(); // no live data: append nothing rather than invent readings

        var nowIst = IstClock.Now;
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
        var nowIst = IstClock.Now;

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
            actuator.LastTriggered = IstClock.Now;
            await context.SaveChangesAsync();
        }
    }
}
