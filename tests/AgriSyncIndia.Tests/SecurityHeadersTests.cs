using AgriSyncIndia.Services;
using Xunit;

namespace AgriSyncIndia.Tests;

public class SecurityHeadersTests
{
    [Fact]
    public void Scripts_and_styles_are_self_only_without_clerk()
    {
        var csp = SecurityHeaders.ContentSecurityPolicy("");
        Assert.Contains("script-src 'self';", csp);
        Assert.Contains("style-src 'self';", csp);
        Assert.DoesNotContain("unsafe-inline", csp);
        Assert.DoesNotContain("jsdelivr", csp);
        Assert.DoesNotContain("googleapis", csp);
        Assert.DoesNotContain("gstatic", csp);
    }

    [Fact]
    public void Clerk_origin_is_added_to_script_connect_frame_only_when_configured()
    {
        var csp = SecurityHeaders.ContentSecurityPolicy("https://demo.clerk.accounts.dev");
        Assert.Contains("script-src 'self' https://demo.clerk.accounts.dev;", csp);
        Assert.Contains("connect-src 'self' https://demo.clerk.accounts.dev;", csp);
        Assert.Contains("frame-src 'self' https://demo.clerk.accounts.dev;", csp);
        Assert.Contains("style-src 'self';", csp);
    }

    [Fact]
    public void Framing_and_plugins_stay_blocked()
    {
        var csp = SecurityHeaders.ContentSecurityPolicy("");
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.Contains("object-src 'none'", csp);
        Assert.Contains("img-src 'self' data:", csp);
    }
}
