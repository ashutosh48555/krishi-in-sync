namespace AgriSyncIndia.Services;

/// <summary>IST wall-clock with Unspecified kind (Npgsql rejects Utc for timestamp without time zone).</summary>
public static class IstClock
{
    public static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow.AddHours(5.5), DateTimeKind.Unspecified);
}
