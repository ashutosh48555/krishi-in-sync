using AgriSyncIndia.Data;
using AgriSyncIndia.Models;
using AgriSyncIndia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriSyncIndia.Controllers.Api;

/// <summary>
/// RESTful Web API for Agricultural Zones & Telemetry endpoints (CO6).
/// Testable via Postman or curl.
/// </summary>
[ApiController]
[Route("api/zones")]
[Produces("application/json")]
public class ZonesApiController : ControllerBase
{
    private readonly AgriSyncDbContext _context;

    public ZonesApiController(AgriSyncDbContext context)
    {
        _context = context;
    }

    /// <summary>GET /api/zones - List all zones with their latest telemetry reading.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetZones()
    {
        var zones = await _context.Zones.AsNoTracking().ToListAsync();
        var result = new List<object>();

        foreach (var z in zones)
        {
            var latestLog = await _context.SensorLogs.AsNoTracking()
                .Where(s => s.ZoneId == z.ZoneId)
                .OrderByDescending(s => s.TimestampIST)
                .FirstOrDefaultAsync();

            result.Add(new
            {
                z.ZoneId,
                z.Name,
                z.CropType,
                z.TargetTemp,
                z.TargetHumidity,
                z.Status,
                z.AreaSqMeters,
                LatestTelemetry = latestLog == null ? null : new
                {
                    latestLog.Temperature,
                    latestLog.Humidity,
                    latestLog.VPD,
                    latestLog.SoilMoisture,
                    latestLog.SoilTemperature,
                    latestLog.TimestampIST
                }
            });
        }

        return Ok(result);
    }

    /// <summary>GET /api/zones/{id} - Get a specific zone and latest metrics.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetZone(int id)
    {
        var z = await _context.Zones.AsNoTracking().FirstOrDefaultAsync(x => x.ZoneId == id);
        if (z == null)
            return NotFound(new { error = $"Zone #{id} not found." });

        var latestLog = await _context.SensorLogs.AsNoTracking()
            .Where(s => s.ZoneId == z.ZoneId)
            .OrderByDescending(s => s.TimestampIST)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            z.ZoneId,
            z.Name,
            z.CropType,
            z.TargetTemp,
            z.TargetHumidity,
            z.Status,
            z.Description,
            z.AreaSqMeters,
            LatestTelemetry = latestLog == null ? null : new
            {
                latestLog.Temperature,
                latestLog.Humidity,
                latestLog.VPD,
                latestLog.SoilMoisture,
                latestLog.SoilTemperature,
                latestLog.TimestampIST
            }
        });
    }

    /// <summary>GET /api/zones/{id}/telemetry?hours=24 - Get historical sensor telemetry.</summary>
    [HttpGet("{id:int}/telemetry")]
    public async Task<ActionResult<IEnumerable<SensorLog>>> GetZoneTelemetry(int id, [FromQuery] int hours = 24)
    {
        if (!await _context.Zones.AnyAsync(z => z.ZoneId == id))
            return NotFound(new { error = $"Zone #{id} not found." });

        var cutoff = IstClock.Now.AddHours(-Math.Clamp(hours, 1, 168));
        var logs = await _context.SensorLogs.AsNoTracking()
            .Where(s => s.ZoneId == id && s.TimestampIST >= cutoff)
            .OrderByDescending(s => s.TimestampIST)
            .ToListAsync();

        return Ok(logs);
    }
}
