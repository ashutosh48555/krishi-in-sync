// Inserts one SensorLogs row for ZoneId=1 with TEMP degrees. Used by tools/prove-live.mjs
// to prove Realtime pushes a committed write to the open page.
using Npgsql;

static string ToNpgsql(string raw)
{
    if (!raw.StartsWith("postgres")) return raw;
    var u = new Uri(raw);
    return new NpgsqlConnectionStringBuilder
    {
        Host = u.Host,
        Port = 5432,
        Database = u.AbsolutePath.Trim('/'),
        Username = u.UserInfo.Split(':')[0],
        Password = u.UserInfo.Split(':', 2)[1],
        SslMode = SslMode.Require,
    }.ConnectionString;
}

var temp = double.Parse(Environment.GetEnvironmentVariable("TEMP")!);
await using var connection = new NpgsqlConnection(ToNpgsql(Environment.GetEnvironmentVariable("U")!));
await connection.OpenAsync();
await using var command = new NpgsqlCommand(
    """
    INSERT INTO "SensorLogs"
      ("ZoneId","Temperature","Humidity","VPD","SoilMoisture","SolarRadiation","SoilTemperature","WindSpeed","TimestampIST")
    VALUES (1, $1, 55.5, 1.11, 42.4, 300, 24.1, 2.2, now() at time zone 'Asia/Kolkata')
    RETURNING "LogId"
    """, connection);
command.Parameters.AddWithValue(temp);
Console.WriteLine($"inserted LogId={await command.ExecuteScalarAsync()} temp={temp}");
