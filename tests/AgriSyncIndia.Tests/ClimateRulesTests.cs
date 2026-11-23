using AgriSyncIndia.Services;
using Xunit;

namespace AgriSyncIndia.Tests;

public class ClimateRulesTests
{
    [Fact]
    public void Vpd_matches_reference_value()
        => Assert.InRange(ClimateRules.Vpd(24.2, 68), 0.95, 1.00);

    [Fact]
    public void Vpd_is_zero_at_saturation()
        => Assert.Equal(0, ClimateRules.Vpd(25, 100), 6);

    [Theory]
    [InlineData(24.5, 70.0, 24.5, 70.0, ZoneStatus.OnTarget)]
    [InlineData(26.0, 70.0, 24.5, 70.0, ZoneStatus.OnTarget)]   // exactly +1.5
    [InlineData(26.1, 70.0, 24.5, 70.0, ZoneStatus.Above)]
    [InlineData(22.9, 70.0, 24.5, 70.0, ZoneStatus.Below)]
    [InlineData(24.5, 50.0, 24.5, 70.0, ZoneStatus.Dry)]
    [InlineData(24.5, 90.0, 24.5, 70.0, ZoneStatus.Humid)]
    [InlineData(null, null, 24.5, 70.0, ZoneStatus.NoReading)]
    public void Status_uses_thresholds(double? t, double? rh, double tt, double th, ZoneStatus expected)
        => Assert.Equal(expected, ClimateRules.Status(t, rh, tt, th));

    [Theory]
    [InlineData(0.1, VpdBand.Low)]
    [InlineData(0.4, VpdBand.Vegetative)]
    [InlineData(0.79, VpdBand.Vegetative)]
    [InlineData(0.8, VpdBand.Optimal)]
    [InlineData(1.25, VpdBand.Optimal)]
    [InlineData(1.3, VpdBand.Moderate)]
    [InlineData(1.6, VpdBand.Moderate)]
    [InlineData(1.7, VpdBand.Severe)]
    public void Band_edges(double vpd, VpdBand expected)
        => Assert.Equal(expected, ClimateRules.Band(vpd));

    static readonly ZoneState[] Mixed =
    {
        new("Polyhouse Nursery", ZoneStatus.Above, 2.4),
        new("Open Field", ZoneStatus.OnTarget, 0.1),
        new("Drip Sector", ZoneStatus.OnTarget, -0.2),
    };

    [Fact]
    public void Headline_en_mixed()
        => Assert.Equal("Polyhouse Nursery is 2.4 °C above target. Open Field and Drip Sector are on target.",
            ClimateRules.Headline("en", Mixed));

    [Fact]
    public void Headline_all_on_target_all_langs()
    {
        var z = new ZoneState[] { new("A", ZoneStatus.OnTarget, 0), new("B", ZoneStatus.OnTarget, 0) };
        Assert.Equal("All zones are on target.", ClimateRules.Headline("en", z));
        Assert.Contains("लक्ष्य", ClimateRules.Headline("hi", z));
        Assert.Contains("ਟੀਚੇ", ClimateRules.Headline("pa", z));
    }

    [Theory]
    [InlineData("en")] [InlineData("hi")] [InlineData("pa")]
    public void Headline_no_data(string lang)
    {
        var z = new ZoneState[] { new("A", ZoneStatus.NoReading, 0) };
        Assert.False(string.IsNullOrWhiteSpace(ClimateRules.Headline(lang, z)));
        Assert.False(string.IsNullOrWhiteSpace(ClimateRules.Headline(lang, Array.Empty<ZoneState>())));
    }

    [Theory]
    [InlineData("en")] [InlineData("hi")] [InlineData("pa")]
    public void Headline_covers_every_status(string lang)
    {
        foreach (var s in new[] { ZoneStatus.Above, ZoneStatus.Below, ZoneStatus.Dry, ZoneStatus.Humid, ZoneStatus.NoReading })
        {
            var h = ClimateRules.Headline(lang, new ZoneState[] { new("Z", s, -1.8), new("Y", ZoneStatus.OnTarget, 0) });
            Assert.Contains("Z", h);
        }
    }

    [Fact]
    public void Headline_below_uses_absolute_delta()
        => Assert.Contains("1.8 °C below", ClimateRules.Headline("en", new ZoneState[] { new("Z", ZoneStatus.Below, -1.8) }));

    [Theory]
    [InlineData(ZoneStatus.Above, "Open the polyhouse vents and run the fan until evening.")]
    [InlineData(ZoneStatus.Below, "Close the polyhouse vents to hold heat.")]
    [InlineData(ZoneStatus.OnTarget, "No action needed today.")]
    [InlineData(ZoneStatus.NoReading, "No action needed today.")]
    public void Advice_en(ZoneStatus s, string expected)
        => Assert.Equal(expected, ClimateRules.Advice("en", s, 2.4, 24.5).Text);

    [Fact]
    public void Advice_translated_and_reason()
    {
        var hi = ClimateRules.Advice("hi", ZoneStatus.Above, 2.4, 24.5);
        var pa = ClimateRules.Advice("pa", ZoneStatus.Below, -2.0, 24.5);
        Assert.NotEqual("Open the polyhouse vents and run the fan until evening.", hi.Text);
        Assert.NotEmpty(pa.Text);
        Assert.Contains("2.4", hi.Reason);
        Assert.Contains("24.5", ClimateRules.Advice("en", ZoneStatus.Above, 2.4, 24.5).Reason);
        Assert.NotEmpty(ClimateRules.Advice("en", ZoneStatus.OnTarget, 0, 24.5).Reason);
        Assert.NotEmpty(ClimateRules.Advice("hi", ZoneStatus.OnTarget, 0, 24.5).Reason);
        Assert.NotEmpty(ClimateRules.Advice("pa", ZoneStatus.OnTarget, 0, 24.5).Reason);
    }
}
