using System.ComponentModel.DataAnnotations;
using AgriSyncIndia.Models;

namespace AgriSyncIndia.ViewModels;

public class AutomationViewModel
{
    // Soil Telemetry Section
    public SoilHealthMetrics SoilMetrics { get; set; } = new();

    // Form Binding for adding a new IF-THEN rule
    public AutomationRuleInputModel NewRule { get; set; } = new();

    // Existing Rules
    public List<AutomationRule> ExistingRules { get; set; } = new();

    // Available Actuators
    public List<Actuator> Actuators { get; set; } = new();

    // Flash message
    public string? StatusMessage { get; set; }
}

public class SoilHealthMetrics
{
    public double VolumetricWaterContent { get; set; } = 42.5; // %
    public double RootZoneTemperature { get; set; } = 22.4; // °C
    public double FieldCapacity { get; set; } = 65.0; // %
    public double PermanentWiltingPoint { get; set; } = 20.0; // %
    public double PlantAvailableWater { get; set; } = 50.0; // %
    public string IrrigationStatus { get; set; } = "Adequate Moisture";
    public string SoilTextureType { get; set; } = "Indo-Gangetic Alluvial Silt Loam";
    public double SalinityEC { get; set; } = 0.82; // dS/m
}

public class AutomationRuleInputModel
{
    [Required(ErrorMessage = "Please select a condition variable.")]
    [Display(Name = "Condition Variable")]
    public string ConditionVariable { get; set; } = "Relative Humidity";

    [Required(ErrorMessage = "Please select a comparison operator.")]
    [Display(Name = "Operator")]
    public string Operator { get; set; } = ">";

    [Required(ErrorMessage = "Please provide a numeric threshold value.")]
    [Range(-50.0, 150.0, ErrorMessage = "Threshold must be between -50 and 150.")]
    [Display(Name = "Threshold Value")]
    public double ThresholdValue { get; set; } = 75.0;

    [Required(ErrorMessage = "Please select or type an action target.")]
    [StringLength(100)]
    [Display(Name = "Action Target")]
    public string ActionTarget { get; set; } = "Turn ON Exhaust Fans";

    [Display(Name = "Enable Immediately")]
    public bool IsActive { get; set; } = true;

    [StringLength(200)]
    [Display(Name = "Rule Description")]
    public string? Description { get; set; }
}
