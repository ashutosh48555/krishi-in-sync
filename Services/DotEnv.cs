namespace AgriSyncIndia.Services;

/// <summary>
/// Minimal dotenv reader for the files `neon env pull` writes (.env, then .env.local).
/// Real environment variables always win, so a deployed host (Render) never depends on a file.
/// Nothing here logs a value.
/// </summary>
public static class DotEnv
{
    public static IDictionary<string, string?> Read(params string[] paths)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;

            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#')) continue;

                if (line.StartsWith("export ", StringComparison.Ordinal))
                    line = line["export ".Length..].TrimStart();

                var separator = line.IndexOf('=');
                if (separator <= 0) continue;

                var key = line[..separator].Trim();
                var value = line[(separator + 1)..].Trim();

                // Strip one layer of matching quotes, keep everything else verbatim
                // (connection strings contain ';' and '\' that must not be touched).
                if (value.Length >= 2 &&
                    ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
                    value = value[1..^1];

                values[key] = value;
            }
        }

        return values;
    }
}
