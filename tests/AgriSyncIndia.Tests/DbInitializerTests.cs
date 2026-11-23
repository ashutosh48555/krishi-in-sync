using AgriSyncIndia.Data;
using AgriSyncIndia.Models;
using AgriSyncIndia.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AgriSyncIndia.Tests;

public class DbInitializerTests
{
    [Fact]
    public async Task Initialize_twice_is_idempotent()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<AgriSyncDbContext>().UseSqlite(conn).Options;
        var sp = new ServiceCollection().BuildServiceProvider();
        using var ctx = new AgriSyncDbContext(options);

        await ctx.Database.EnsureCreatedAsync();
        await DbInitializer.InitializeAsync(ctx, sp);
        var first = await Counts(ctx);
        await DbInitializer.InitializeAsync(ctx, sp);

        Assert.Equal(first, await Counts(ctx));
        Assert.Equal((3, 504, 4, 3, 2, 3, 4), first);
    }

    static async Task<(int, int, int, int, int, int, int)> Counts(AgriSyncDbContext c) =>
        (await c.Zones.CountAsync(), await c.SensorLogs.CountAsync(), await c.AutomationRules.CountAsync(),
         await c.ClimateAlerts.CountAsync(), await c.Users.CountAsync(), await c.Actuators.CountAsync(),
         await c.FarmClusters.CountAsync());
}
