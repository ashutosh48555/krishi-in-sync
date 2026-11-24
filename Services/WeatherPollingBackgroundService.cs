namespace AgriSyncIndia.Services;

/// <summary>
/// Autonomous background service (IHostedService) that periodically polls
/// Open-Meteo REST API, records microclimate telemetry, and triggers automated rules.
/// Runs unattended without requiring user interactions (INT402 autonomous architecture).
/// </summary>
public sealed class WeatherPollingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WeatherPollingBackgroundService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromMinutes(15);

    public WeatherPollingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<WeatherPollingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WeatherPollingBackgroundService started. Periodic interval: {Minutes}m.", _period.TotalMinutes);

        // Initial grace delay to ensure DB migrations and seed data have initialized cleanly
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        using var timer = new PeriodicTimer(_period);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var weatherService = scope.ServiceProvider.GetRequiredService<IOpenMeteoService>();
                var logs = await weatherService.IngestAndSyncTelemetryAsync();
                _logger.LogInformation("Autonomous background weather sync: {Count} zone logs recorded at {Time} IST.",
                    logs.Count, IstClock.Now.ToString("HH:mm:ss"));
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Background weather sync encountered a transient issue. Will retry in {Minutes}m.", _period.TotalMinutes);
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }
    }
}
