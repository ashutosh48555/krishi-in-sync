using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriSyncIndia.Migrations
{
    /// <summary>
    /// Neon deployment: Realtime rejects live queries on tables with row-level security enabled.
    /// RLS exists only to block Supabase's public PostgREST surface, which Neon does not expose
    /// (its Data API is off), and the app always connects as the table owner anyway.
    /// Postgres-only; nothing to do on SQLite.
    /// </summary>
    public partial class DisableRlsForRealtime : Migration
    {
        private static readonly string[] Tables =
        {
            "Actuators", "AutomationRules", "ClimateAlerts", "DataProtectionKeys", "FarmClusters",
            "SensorLogs", "Users", "Zones", "__EFMigrationsHistory"
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL") return;

            foreach (var table in Tables)
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" DISABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL") return;

            foreach (var table in Tables)
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ENABLE ROW LEVEL SECURITY;");
        }
    }
}
