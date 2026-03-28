using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SiteSettingShipmentOpsThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShipmentAttentionDelayHours",
                schema: "Settings",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShipmentTrackingGraceHours",
                schema: "Settings",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShipmentAttentionDelayHours",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ShipmentTrackingGraceHours",
                schema: "Settings",
                table: "SiteSettings");
        }
    }
}
