using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using AgriSyncIndia.Models;
using AgriSyncIndia.Services;

namespace AgriSyncIndia.ViewModels;

public class AutomationViewModel
{
    public SoilHealthMetrics SoilMetrics { get; set; } = new();
    public AutomationRuleInputModel NewRule { get; set; } = new();
    public List<AutomationRule> ExistingRules { get; set; } = new();
    public List<Actuator> Actuators { get; set; } = new();
}

/// <summary>Latest Drip Sector soil reading. Null values mean "no reading yet" (never a default shown as data).</summary>
public class SoilHealthMetrics
{
    public double? VolumetricWaterContent { get; set; }
    public double? RootZoneTemperature { get; set; }

    /// <summary>Translation key suffix (<c>soil.*</c>) and tone ("", "watch", "crit").</summary>
    public (string Key, string Tone)? Status => VolumetricWaterContent switch
    {
        null => null,
        < 30.0 => ("stress", "crit"),
        < 45.0 => ("low", "watch"),
        <= 75.0 => ("ok", ""),
        _ => ("wet", "watch")
    };
}

/// <summary>Create-rule form. <c>ErrorMessage</c> values are keys in <see cref="Strings"/>; the view translates them.</summary>
public class AutomationRuleInputModel : IValidatableObject
{
    public static readonly string[] Metrics = { "Temperature", "Relative Humidity", "VPD" };

    [Required(ErrorMessage = "val.metric")]
    [RegularExpression("Temperature|Relative Humidity|VPD", ErrorMessage = "val.metric")]
    public string ConditionVariable { get; set; } = "Temperature";

    [Required(ErrorMessage = "val.op")]
    [RegularExpression(@"^(>|<|>=|<=)$", ErrorMessage = "val.op")]
    public string Operator { get; set; } = ">";

    [Required(ErrorMessage = "val.thr.req")]
    public double? ThresholdValue { get; set; }

    /// <summary>Which actuator to switch (its zone comes with it). Null only for callers that set <see cref="ActionTarget"/> directly.</summary>
    [Required(ErrorMessage = "val.actuator")]
    public int? ActuatorId { get; set; }

    public bool TurnOn { get; set; } = true;

    /// <summary>Built by the controller from the actuator and <see cref="TurnOn"/>; the evaluator matches on this text.</summary>
    [StringLength(100)]
    public string ActionTarget { get; set; } = "Turn ON Exhaust Fans";

    public bool IsActive { get; set; } = true;

    [StringLength(200, ErrorMessage = "val.note")]
    public string? Description { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (ThresholdValue is double v && Array.IndexOf(Metrics, ConditionVariable) >= 0)
        {
            var (min, max) = RuleDisplay.Range(ConditionVariable);
            if (double.IsNaN(v) || v < min || v > max)
                yield return new ValidationResult("val.thr." + RuleDisplay.Slug(ConditionVariable), new[] { nameof(ThresholdValue) });
        }
    }
}

/// <summary>Text and limits for a rule. Pure functions so they can be tested without a web host.</summary>
public static class RuleDisplay
{
    public static string Slug(string conditionVariable) => conditionVariable.ToLowerInvariant() switch
    {
        "relative humidity" or "humidity" or "rh" => "hum",
        "vpd" => "vpd",
        "soil moisture" => "soil",
        _ => "temp"
    };

    public static string Unit(string conditionVariable) => Slug(conditionVariable) switch
    {
        "temp" => "°C", "vpd" => "kPa", _ => "%"
    };

    public static (double Min, double Max) Range(string conditionVariable) => Slug(conditionVariable) switch
    {
        "temp" => (-10, 60), "vpd" => (0, 5), _ => (0, 100)
    };

    static string Num(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    static string OpKey(string op) => op switch { ">" => "gt", "<" => "lt", ">=" => "ge", "<=" => "le", _ => "eq" };

    public static bool TurnsOn(string actionTarget) => actionTarget.Contains("Turn ON", StringComparison.OrdinalIgnoreCase);

    /// <summary>Device keyword used by the rule evaluator (Exhaust, Misting, Drip), or null.</summary>
    public static string? DeviceSlug(string text) =>
        text.Contains("Exhaust", StringComparison.OrdinalIgnoreCase) ? "exhaust"
        : text.Contains("Misting", StringComparison.OrdinalIgnoreCase) ? "misting"
        : text.Contains("Drip", StringComparison.OrdinalIgnoreCase) ? "drip" : null;

    public static string Device(string lang, string nameOrTarget) =>
        DeviceSlug(nameOrTarget) is string s ? Localizer.T(lang, "dev." + s) : nameOrTarget;

    public static string ZoneName(string lang, string zoneName)
    {
        var key = zoneName.Contains("Polyhouse", StringComparison.OrdinalIgnoreCase) ? "poly"
            : zoneName.Contains("Open", StringComparison.OrdinalIgnoreCase) ? "open"
            : zoneName.Contains("Drip", StringComparison.OrdinalIgnoreCase) ? "drip" : "";
        return key.Length > 0 ? Localizer.T(lang, "zone." + key) : zoneName;
    }

    /// <summary>"If temperature is above 28 °C, turn on Exhaust fans".</summary>
    public static string Sentence(string lang, AutomationRule r)
    {
        var cond = string.Format(CultureInfo.InvariantCulture, Localizer.T(lang, "rule.if." + OpKey(r.Operator)),
            Localizer.T(lang, "rm." + Slug(r.ConditionVariable)), Num(r.ThresholdValue) + " " + Unit(r.ConditionVariable));
        var then = string.Format(CultureInfo.InvariantCulture, Localizer.T(lang, TurnsOn(r.ActionTarget) ? "rule.then.on" : "rule.then.off"),
            Device(lang, r.ActionTarget));
        return string.Format(CultureInfo.InvariantCulture, Localizer.T(lang, "rule.sentence"), cond, then);
    }

    /// <summary>The actuator a rule drives, found the same way the evaluator finds it (by device keyword).</summary>
    public static Actuator? ActuatorFor(IEnumerable<Actuator> actuators, string actionTarget) =>
        DeviceSlug(actionTarget) is string s ? actuators.FirstOrDefault(a => DeviceSlug(a.Name) == s) : null;

    public static string ActionTargetFor(Actuator a, bool turnOn) => $"Turn {(turnOn ? "ON" : "OFF")} {a.Name}";
}

/// <summary>What a user may do and, if not, the translation key that says why.</summary>
public readonly record struct ControlAccess(bool Allowed, string? ReasonKey)
{
    public static ControlAccess ForActuators(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated != true ? new(false, "au.act.signin")
        : Permissions.IsOperator(user) ? new(true, null) : new(false, "au.act.why");

    public static ControlAccess ForRule(ClaimsPrincipal user, AutomationRule rule) =>
        Permissions.CanModify(user, rule) ? new(true, null)
        : new(false, user.Identity?.IsAuthenticated == true ? "au.ro.why" : "au.ro.signin");

    public static string Owner(ClaimsPrincipal user, AutomationRule rule) =>
        rule.OwnerUserId == null ? "seed" : rule.OwnerUserId == Permissions.UserId(user) ? "you" : "user";
}

/// <summary>Turns a ModelState error (a translation key, or a framework message) into text in the page language.</summary>
public static class FormText
{
    public static string Error(string lang, string message) =>
        Strings.All.ContainsKey(message) ? Localizer.T(lang, message) : Localizer.T(lang, "val.invalid");
}
