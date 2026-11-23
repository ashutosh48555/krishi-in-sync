using System.Globalization;
using AgriSyncIndia.ViewModels;

namespace AgriSyncIndia.Services;

/// <summary>Small formatting helpers shared by the Razor views (invariant culture so "27.4" is never "27,4").</summary>
public static class ViewFormat
{
    public static string N(double? v, int decimals = 1) =>
        v is null ? "—" : v.Value.ToString("F" + decimals, CultureInfo.InvariantCulture);

    /// <summary>"" (good), "watch" or "crit": the status colour for a VPD band.</summary>
    public static string Tone(VpdBand? band) => band switch
    {
        VpdBand.Severe => "crit",
        VpdBand.Low or VpdBand.Moderate => "watch",
        _ => ""
    };

    public static string Tone(string severity) => severity switch { "Critical" => "crit", "Warning" => "watch", _ => "good" };

    /// <summary>Marker position on the 0 to 2 kPa track, in percent.</summary>
    public static string Pos(double? vpd) => N(Math.Clamp((vpd ?? 0) / 2.0 * 100.0, 0, 100), 1);

    public static string Band(string lang, VpdBand? b) => b is null ? Localizer.T(lang, "m.noreading") : Localizer.T(lang, "vpd." + b);

    /// <summary>Plain sentence for the temperature gap, e.g. "2.4 °C above target".</summary>
    public static string Gap(string lang, ZoneCardViewModel c)
    {
        if (c.Delta is not double d) return Localizer.T(lang, "m.noreading");
        if (Math.Abs(d) <= Services.ClimateRules.TempTolerance) return Localizer.T(lang, "m.ontarget");
        return string.Format(CultureInfo.InvariantCulture, Localizer.T(lang, d > 0 ? "m.above" : "m.below"), N(Math.Abs(d)));
    }
}
