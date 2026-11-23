using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriSyncIndia.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "AutomationRules",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "AutomationRules");
        }
    }
}
