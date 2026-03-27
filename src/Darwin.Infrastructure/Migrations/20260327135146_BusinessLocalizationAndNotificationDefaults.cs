using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BusinessLocalizationAndNotificationDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CustomerEmailNotificationsEnabled",
                schema: "Businesses",
                table: "Businesses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CustomerMarketingEmailsEnabled",
                schema: "Businesses",
                table: "Businesses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DefaultTimeZoneId",
                schema: "Businesses",
                table: "Businesses",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "OperationalAlertEmailsEnabled",
                schema: "Businesses",
                table: "Businesses",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerEmailNotificationsEnabled",
                schema: "Businesses",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "CustomerMarketingEmailsEnabled",
                schema: "Businesses",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "DefaultTimeZoneId",
                schema: "Businesses",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "OperationalAlertEmailsEnabled",
                schema: "Businesses",
                table: "Businesses");
        }
    }
}
