using AgriSyncIndia.Data;
using AgriSyncIndia.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Services;

public partial class OpenMeteoService
{
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
