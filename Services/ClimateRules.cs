using System.Globalization;

namespace AgriSyncIndia.Services;

public enum ZoneStatus { OnTarget, Above, Below, Dry, Humid, NoReading }

public enum VpdBand { Low, Vegetative, Optimal, Moderate, Severe }

public record ZoneState(string Name, ZoneStatus Status, double Delta);

public record AdviceResult(string Text, string Reason);

/// <summary>Pure climate logic: VPD, zone status, headline, advice. No I/O.</summary>
public static class ClimateRules
{
    public const double TempTolerance = 1.5;      // degrees C either side of target
    public const double HumidityTolerance = 10.0; // percentage points either side of target
    public const double VpdLowMax = 0.4;
    public const double VpdVegetativeMax = 0.8;
    public const double VpdOptimalMax = 1.25;
    public const double VpdModerateMax = 1.6;
    public const double NoReadingAfterHours = 3;

    public static double Vpd(double tempC, double rhPercent)
    {
        double es = 0.6108 * Math.Exp(17.27 * tempC / (tempC + 237.3));
        return es * (1 - rhPercent / 100.0);
    }

    public static VpdBand Band(double vpd) => vpd switch
    {
        < VpdLowMax => VpdBand.Low,
        < VpdVegetativeMax => VpdBand.Vegetative,
        <= VpdOptimalMax => VpdBand.Optimal,
        <= VpdModerateMax => VpdBand.Moderate,
        _ => VpdBand.Severe
    };

    public static ZoneStatus Status(double? temp, double? rh, double targetTemp, double targetRh)
    {
        if (temp is null || rh is null) return ZoneStatus.NoReading;
        double dt = Math.Round(temp.Value - targetTemp, 1);
        if (dt > TempTolerance) return ZoneStatus.Above;
        if (dt < -TempTolerance) return ZoneStatus.Below;
        double dh = rh.Value - targetRh;
        if (dh > HumidityTolerance) return ZoneStatus.Humid;
        if (dh < -HumidityTolerance) return ZoneStatus.Dry;
        return ZoneStatus.OnTarget;
    }

    private record Words(string Above, string Below, string Dry, string Humid, string NoReading,
        string OnOne, string OnMany, string And, string AllOn, string NoData);

    private static Words WordsFor(string lang) => lang switch
    {
        "hi" => new("{0} लक्ष्य से {1} °C ऊपर है।", "{0} लक्ष्य से {1} °C नीचे है।", "{0} लक्ष्य से सूखा है।",
            "{0} लक्ष्य से अधिक नम है।", "{0} की रीडिंग अभी नहीं है।", "{0} लक्ष्य पर है।", "{0} लक्ष्य पर हैं।",
            " और ", "सभी क्षेत्र लक्ष्य पर हैं।", "अभी कोई रीडिंग नहीं है।"),
        "pa" => new("{0} ਟੀਚੇ ਤੋਂ {1} °C ਉੱਪਰ ਹੈ।", "{0} ਟੀਚੇ ਤੋਂ {1} °C ਹੇਠਾਂ ਹੈ।", "{0} ਟੀਚੇ ਤੋਂ ਸੁੱਕਾ ਹੈ।",
            "{0} ਟੀਚੇ ਤੋਂ ਵੱਧ ਨਮੀ ਵਾਲਾ ਹੈ।", "{0} ਦੀ ਰੀਡਿੰਗ ਹਾਲੇ ਨਹੀਂ ਹੈ।", "{0} ਟੀਚੇ 'ਤੇ ਹੈ।", "{0} ਟੀਚੇ 'ਤੇ ਹਨ।",
            " ਅਤੇ ", "ਸਾਰੇ ਜ਼ੋਨ ਟੀਚੇ 'ਤੇ ਹਨ।", "ਹਾਲੇ ਕੋਈ ਰੀਡਿੰਗ ਨਹੀਂ।"),
        _ => new("{0} is {1} °C above target.", "{0} is {1} °C below target.", "{0} is drier than target.",
            "{0} is more humid than target.", "{0} has no reading yet.", "{0} is on target.", "{0} are on target.",
            " and ", "All zones are on target.", "No readings yet.")
    };

    public static string Headline(string lang, IReadOnlyList<ZoneState> zones)
    {
        var w = WordsFor(lang);
        if (zones.Count == 0 || zones.All(z => z.Status == ZoneStatus.NoReading)) return w.NoData;
        if (zones.All(z => z.Status == ZoneStatus.OnTarget)) return w.AllOn;

        var parts = new List<string>();
        foreach (var z in zones.Where(z => z.Status != ZoneStatus.OnTarget))
        {
            string d = Math.Abs(z.Delta).ToString("0.0", CultureInfo.InvariantCulture);
            string fmt = z.Status switch
            {
                ZoneStatus.Above => w.Above,
                ZoneStatus.Below => w.Below,
                ZoneStatus.Dry => w.Dry,
                ZoneStatus.Humid => w.Humid,
                _ => w.NoReading
            };
            parts.Add(string.Format(CultureInfo.InvariantCulture, fmt, z.Name, d));
        }

        var on = zones.Where(z => z.Status == ZoneStatus.OnTarget).Select(z => z.Name).ToList();
        if (on.Count > 0)
            parts.Add(string.Format(CultureInfo.InvariantCulture, on.Count == 1 ? w.OnOne : w.OnMany,
                string.Join(w.And, on)));
        return string.Join(" ", parts);
    }

    /// <summary>Advice comes from the polyhouse status only (PRODUCT.md honesty rules).</summary>
    public static AdviceResult Advice(string lang, ZoneStatus polyhouse, double delta, double targetTemp)
    {
        string d = Math.Abs(delta).ToString("0.0", CultureInfo.InvariantCulture);
        string t = targetTemp.ToString("0.0", CultureInfo.InvariantCulture);
        return (polyhouse, lang) switch
        {
            (ZoneStatus.Above, "hi") => new("पॉलीहाउस के वेंट खोलें और शाम तक पंखा चलाएं।", $"पॉलीहाउस {t} °C लक्ष्य से {d} °C ऊपर है (मॉडल आधारित)।"),
            (ZoneStatus.Above, "pa") => new("ਪੌਲੀਹਾਊਸ ਦੇ ਵੈਂਟ ਖੋਲ੍ਹੋ ਅਤੇ ਸ਼ਾਮ ਤੱਕ ਪੱਖਾ ਚਲਾਓ।", $"ਪੌਲੀਹਾਊਸ {t} °C ਟੀਚੇ ਤੋਂ {d} °C ਉੱਪਰ ਹੈ (ਮਾਡਲ ਅਧਾਰਿਤ)।"),
            (ZoneStatus.Above, _) => new("Open the polyhouse vents and run the fan until evening.", $"Polyhouse is {d} °C above its {t} °C target (modelled)."),
            (ZoneStatus.Below, "hi") => new("गर्मी बनाए रखने के लिए पॉलीहाउस के वेंट बंद करें।", $"पॉलीहाउस {t} °C लक्ष्य से {d} °C नीचे है (मॉडल आधारित)।"),
            (ZoneStatus.Below, "pa") => new("ਗਰਮੀ ਬਣਾਈ ਰੱਖਣ ਲਈ ਪੌਲੀਹਾਊਸ ਦੇ ਵੈਂਟ ਬੰਦ ਕਰੋ।", $"ਪੌਲੀਹਾਊਸ {t} °C ਟੀਚੇ ਤੋਂ {d} °C ਹੇਠਾਂ ਹੈ (ਮਾਡਲ ਅਧਾਰਿਤ)।"),
            (ZoneStatus.Below, _) => new("Close the polyhouse vents to hold heat.", $"Polyhouse is {d} °C below its {t} °C target (modelled)."),
            (_, "hi") => new("आज किसी कार्रवाई की जरूरत नहीं।", "पॉलीहाउस का तापमान लक्ष्य सीमा में है, या रीडिंग उपलब्ध नहीं है।"),
            (_, "pa") => new("ਅੱਜ ਕਿਸੇ ਕਾਰਵਾਈ ਦੀ ਲੋੜ ਨਹੀਂ।", "ਪੌਲੀਹਾਊਸ ਦਾ ਤਾਪਮਾਨ ਟੀਚੇ ਦੀ ਸੀਮਾ ਵਿੱਚ ਹੈ, ਜਾਂ ਰੀਡਿੰਗ ਉਪਲਬਧ ਨਹੀਂ।"),
            _ => new("No action needed today.", "Polyhouse temperature is within range, or there is no reading.")
        };
    }
}
