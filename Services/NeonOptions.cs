namespace AgriSyncIndia.Services;

/// <summary>
/// Public half of the Neon services: what the browser may know.
/// The Realtime secret and the database URL never leave the server; the browser only needs
/// the WebSocket URL and the Function's HTTPS base URL.
/// </summary>
public sealed class NeonOptions
{
    public string? RealtimeUrl { get; set; }
    public string? TelemetryUrl { get; set; }

    /// <summary>wss://... for Realtime, ws:// for a local `neon dev`.</summary>
    public bool RealtimeEnabled => !string.IsNullOrWhiteSpace(RealtimeUrl);

    /// <summary>
    /// <paramref name="env"/> is the parsed dotenv, used only as a fallback: a real environment
    /// variable always wins, so a deployed container without any .env file still gets the URLs.
    /// </summary>
    public static NeonOptions From(IDictionary<string, string?> env)
    {
        string? Read(string key)
        {
            var fromEnvironment = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(fromEnvironment)) return fromEnvironment.Trim();
            return env.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;
        }

        var telemetryUrl = Read("NEON_TELEMETRY_URL");
        if (telemetryUrl is null)
        {
            // A Function answers at https://<branch-id>-<slug><host-suffix>.
            var branchId = Read("NEON_BRANCH_ID");
            var suffix = Read("NEON_FUNCTION_HOST_SUFFIX");
            if (branchId is not null && suffix is not null)
                telemetryUrl = $"https://{branchId}-telemetry{suffix}".TrimEnd('/');
        }

        return new NeonOptions
        {
            RealtimeUrl = Read("NEON_REALTIME_URL"),
            TelemetryUrl = telemetryUrl?.TrimEnd('/'),
        };
    }
}
