using AgriSyncIndia.Services;
using Xunit;

namespace AgriSyncIndia.Tests;

public class IstClockTests
{
    [Fact]
    public void Now_is_unspecified_and_utc_plus_530()
    {
        var now = IstClock.Now;
        Assert.Equal(DateTimeKind.Unspecified, now.Kind);
        var expected = DateTime.UtcNow.AddHours(5.5);
        Assert.True(Math.Abs((now - expected).TotalSeconds) < 5);
    }
}
