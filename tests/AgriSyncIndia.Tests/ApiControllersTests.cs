using AgriSyncIndia.Controllers.Api;
using AgriSyncIndia.Data;
using AgriSyncIndia.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgriSyncIndia.Tests;

public class ApiControllersTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AgriSyncDbContext _db;

    public ApiControllersTests()
    {
        _conn = new SqliteConnection("Data Source=:memory:");
        _conn.Open();
        var options = new DbContextOptionsBuilder<AgriSyncDbContext>().UseSqlite(_conn).Options;
        _db = new AgriSyncDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    [Fact]
    public async Task RulesApi_FullCrudLifecycle()
    {
        var controller = new RulesApiController(_db);

        // 1. Initial GetRules
        var initial = await controller.GetRules();
        var rules = initial.Value ?? (initial.Result as OkObjectResult)?.Value as IEnumerable<AutomationRule>;
        Assert.Empty(rules ?? []);

        // 2. Create Rule (POST)
        var createDto = new RuleCreateDto
        {
            ConditionVariable = "Temperature",
            Operator = ">",
            ThresholdValue = 35.0,
            ActionTarget = "Turn ON Fans",
            Description = "API test rule"
        };
        var createResult = await controller.CreateRule(createDto);
        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var rule = Assert.IsType<AutomationRule>(created.Value);
        Assert.True(rule.RuleId > 0);
        Assert.Equal(35.0, rule.ThresholdValue);

        // 3. Get Rule by ID (GET)
        var getResult = await controller.GetRule(rule.RuleId);
        Assert.NotNull(getResult.Value);
        Assert.Equal("Temperature", getResult.Value.ConditionVariable);

        // 4. Update Rule (PUT)
        var updateDto = new RuleUpdateDto
        {
            ThresholdValue = 38.0,
            Description = "Updated API test rule"
        };
        var updateResult = await controller.UpdateRule(rule.RuleId, updateDto);
        var okResult = Assert.IsType<OkObjectResult>(updateResult);
        var updatedRule = Assert.IsType<AutomationRule>(okResult.Value);
        Assert.Equal(38.0, updatedRule.ThresholdValue);

        // 5. Delete Rule (DELETE)
        var deleteResult = await controller.DeleteRule(rule.RuleId);
        Assert.IsType<NoContentResult>(deleteResult);

        // 6. Verify NotFound after delete
        var notFoundResult = await controller.GetRule(rule.RuleId);
        Assert.IsType<NotFoundObjectResult>(notFoundResult.Result);
    }

    [Fact]
    public async Task ZonesApi_EndpointsReturnData()
    {
        _db.Zones.Add(new Zone
        {
            ZoneId = 1,
            Name = "Polyhouse Alpha",
            CropType = "Tomato",
            TargetTemp = 24.0,
            TargetHumidity = 65.0,
            Status = "Optimal"
        });
        _db.SensorLogs.Add(new SensorLog
        {
            LogId = 1,
            ZoneId = 1,
            Temperature = 25.5,
            Humidity = 66.0,
            VPD = 1.05,
            SoilMoisture = 45.0,
            TimestampIST = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var controller = new ZonesApiController(_db);

        // Test GetZones
        var zonesResult = await controller.GetZones();
        var okZones = Assert.IsType<OkObjectResult>(zonesResult.Result);
        var zonesList = Assert.IsAssignableFrom<System.Collections.IEnumerable>(okZones.Value);
        Assert.NotNull(zonesList);

        // Test GetZone by ID
        var zoneResult = await controller.GetZone(1);
        Assert.IsType<OkObjectResult>(zoneResult.Result);

        // Test Nonexistent Zone
        var notFound = await controller.GetZone(999);
        Assert.IsType<NotFoundObjectResult>(notFound.Result);

        // Test Telemetry
        var telemetryResult = await controller.GetZoneTelemetry(1, 24);
        var okTelemetry = Assert.IsType<OkObjectResult>(telemetryResult.Result);
        var logs = Assert.IsAssignableFrom<IEnumerable<SensorLog>>(okTelemetry.Value);
        Assert.Single(logs);
    }
}
