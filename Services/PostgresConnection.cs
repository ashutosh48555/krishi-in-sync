using Npgsql;

namespace AgriSyncIndia.Services;

/// <summary>
/// Converts the URI form Neon writes to <c>.env.local</c> (postgresql://user:pass@host/db?sslmode=require)
/// into the keyword/value form Npgsql parses. Keyword/value strings pass through untouched.
/// </summary>
public static class PostgresConnection
{
    public static string FromEnvironmentValue(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw ?? string.Empty;

        var trimmed = raw.Trim().Trim('"').Trim('\'');
        if (!trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        var uri = new Uri(trimmed);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
        };

        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length != 2) continue;
            var key = Uri.UnescapeDataString(kv[0]).ToLowerInvariant();
            var value = Uri.UnescapeDataString(kv[1]);

            switch (key)
            {
                case "sslmode":
                    builder.SslMode = value.ToLowerInvariant() switch
                    {
                        "disable" => SslMode.Disable,
                        "allow" => SslMode.Allow,
                        "prefer" => SslMode.Prefer,
                        "require" => SslMode.Require,
                        "verify-ca" => SslMode.VerifyCA,
                        "verify-full" => SslMode.VerifyFull,
                        _ => SslMode.Require,
                    };
                    break;
                case "application_name":
                    builder.ApplicationName = value;
                    break;
                // channel_binding, options, etc. are ignored: Npgsql negotiates these itself.
            }
        }

        return builder.ConnectionString;
    }
}
