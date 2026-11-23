using AgriSyncIndia.Services;
using AgriSyncIndia.ViewModels;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AgriSyncIndia.Tests;

public class ViewFormatAndLocalizerTests
{
    [Fact]
    public void N_FormatsNumbersInvariantly()
    {
        Assert.Equal("—", ViewFormat.N(null));
        Assert.Equal("27.4", ViewFormat.N(27.42, 1));
        Assert.Equal("27.42", ViewFormat.N(27.42, 2));
    }

    [Fact]
    public void Tone_ReturnsExpectedClass()
    {
        Assert.Equal("crit", ViewFormat.Tone(VpdBand.Severe));
        Assert.Equal("watch", ViewFormat.Tone(VpdBand.Low));
        Assert.Equal("watch", ViewFormat.Tone(VpdBand.Moderate));
        Assert.Equal("", ViewFormat.Tone(VpdBand.Optimal));
        Assert.Equal("crit", ViewFormat.Tone("Critical"));
        Assert.Equal("watch", ViewFormat.Tone("Warning"));
        Assert.Equal("good", ViewFormat.Tone("Info"));
    }

    [Fact]
    public void Pos_ClampsPercentage()
    {
        Assert.Equal("0.0", ViewFormat.Pos(-0.5));
        Assert.Equal("50.0", ViewFormat.Pos(1.0));
        Assert.Equal("100.0", ViewFormat.Pos(2.5));
    }

    [Fact]
    public void Band_ReturnsLocalizedText()
    {
        Assert.Equal("No reading yet", ViewFormat.Band("en", null));
        Assert.NotEmpty(ViewFormat.Band("en", VpdBand.Optimal));
        Assert.NotEmpty(ViewFormat.Band("hi", VpdBand.Optimal));
        Assert.NotEmpty(ViewFormat.Band("pa", VpdBand.Optimal));
    }

    [Fact]
    public void Gap_CalculatesAccurately()
    {
        var vmNull = new ZoneCardViewModel { Temp = null, TargetTemp = 25.0, Delta = null };
        Assert.Equal("No reading yet", ViewFormat.Gap("en", vmNull));

        var vmTarget = new ZoneCardViewModel { Temp = 25.5, TargetTemp = 25.0, Delta = 0.5 };
        Assert.Equal("on target", ViewFormat.Gap("en", vmTarget));

        var vmAbove = new ZoneCardViewModel { Temp = 28.5, TargetTemp = 25.0, Delta = 3.5 };
        Assert.Contains("above", ViewFormat.Gap("en", vmAbove));

        var vmBelow = new ZoneCardViewModel { Temp = 22.0, TargetTemp = 25.0, Delta = -3.0 };
        Assert.Contains("below", ViewFormat.Gap("en", vmBelow));
    }

    [Fact]
    public void Localizer_Normalize_And_T()
    {
        Assert.Equal("en", Localizer.Normalize("EN"));
        Assert.Equal("hi", Localizer.Normalize("hi"));
        Assert.Equal("pa", Localizer.Normalize("PA"));
        Assert.Null(Localizer.Normalize("fr"));
        Assert.Null(Localizer.Normalize(null));

        Assert.Equal("Dashboard", Localizer.T("en", "nav.dashboard"));
        Assert.Equal("डैशबोर्ड", Localizer.T("hi", "nav.dashboard"));
        Assert.Equal("ਡੈਸ਼ਬੋਰਡ", Localizer.T("pa", "nav.dashboard"));
        Assert.Equal("missing.key", Localizer.T("en", "missing.key"));
    }

    [Fact]
    public void Localizer_HttpContextExtensions()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items["lang"] = "hi";
        Assert.Equal("hi", ctx.Lang());
        Assert.Equal("डैशबोर्ड", ctx.T("nav.dashboard"));

        ctx.Request.Path = "/Climate/Dashboard";
        ctx.Request.QueryString = new QueryString("?lang=hi&tab=1");
        var urlPa = ctx.LangUrl("pa");
        Assert.Contains("lang=pa", urlPa);
        Assert.Contains("tab=1", urlPa);
    }
}
