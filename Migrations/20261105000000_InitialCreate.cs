using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriSyncIndia.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AutomationRules",
            columns: table => new
            {
                RuleId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ConditionVariable = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Operator = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                ThresholdValue = table.Column<double>(type: "float", nullable: false),
                ActionTarget = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AutomationRules", x => x.RuleId);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                UserId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.UserId);
            });

        migrationBuilder.CreateTable(
            name: "Zones",
            columns: table => new
            {
                ZoneId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                CropType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                TargetTemp = table.Column<double>(type: "float", nullable: false),
                TargetHumidity = table.Column<double>(type: "float", nullable: false),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                AreaSqMeters = table.Column<double>(type: "float", nullable: false),
                IconClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Zones", x => x.ZoneId);
            });

        migrationBuilder.CreateTable(
            name: "Actuators",
            columns: table => new
            {
                ActuatorId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ZoneId = table.Column<int>(type: "int", nullable: false),
                IsAutoMode = table.Column<bool>(type: "bit", nullable: false),
                CurrentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                LastTriggered = table.Column<DateTime>(type: "datetime2", nullable: false),
                DeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                PowerRatingKw = table.Column<double>(type: "float", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Actuators", x => x.ActuatorId);
                table.ForeignKey(
                    name: "FK_Actuators_Zones_ZoneId",
                    column: x => x.ZoneId,
                    principalTable: "Zones",
                    principalColumn: "ZoneId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ClimateAlerts",
            columns: table => new
            {
                AlertId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ZoneId = table.Column<int>(type: "int", nullable: true),
                Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                AgronomistRecommendation = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                TimestampIST = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsAcknowledged = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClimateAlerts", x => x.AlertId);
                table.ForeignKey(
                    name: "FK_ClimateAlerts_Zones_ZoneId",
                    column: x => x.ZoneId,
                    principalTable: "Zones",
                    principalColumn: "ZoneId",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "SensorLogs",
            columns: table => new
            {
                LogId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ZoneId = table.Column<int>(type: "int", nullable: false),
                Temperature = table.Column<double>(type: "float", nullable: false),
                Humidity = table.Column<double>(type: "float", nullable: false),
                VPD = table.Column<double>(type: "float", nullable: false),
                SoilMoisture = table.Column<double>(type: "float", nullable: false),
                SolarRadiation = table.Column<double>(type: "float", nullable: false),
                SoilTemperature = table.Column<double>(type: "float", nullable: false),
                WindSpeed = table.Column<double>(type: "float", nullable: false),
                TimestampIST = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SensorLogs", x => x.LogId);
                table.ForeignKey(
                    name: "FK_SensorLogs_Zones_ZoneId",
                    column: x => x.ZoneId,
                    principalTable: "Zones",
                    principalColumn: "ZoneId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Actuators_ZoneId",
            table: "Actuators",
            column: "ZoneId");

        migrationBuilder.CreateIndex(
            name: "IX_ClimateAlerts_ZoneId",
            table: "ClimateAlerts",
            column: "ZoneId");

        migrationBuilder.CreateIndex(
            name: "IX_SensorLogs_TimestampIST",
            table: "SensorLogs",
            column: "TimestampIST");

        migrationBuilder.CreateIndex(
            name: "IX_SensorLogs_ZoneId_TimestampIST",
            table: "SensorLogs",
            columns: new[] { "ZoneId", "TimestampIST" });

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Actuators");
        migrationBuilder.DropTable(name: "AutomationRules");
        migrationBuilder.DropTable(name: "ClimateAlerts");
        migrationBuilder.DropTable(name: "SensorLogs");
        migrationBuilder.DropTable(name: "Users");
        migrationBuilder.DropTable(name: "Zones");
    }
}
