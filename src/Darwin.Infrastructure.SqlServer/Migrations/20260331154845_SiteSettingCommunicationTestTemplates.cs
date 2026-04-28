using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class SiteSettingCommunicationTestTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommunicationTestEmailBodyTemplate",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommunicationTestEmailSubjectTemplate",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommunicationTestSmsTemplate",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommunicationTestWhatsAppTemplate",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommunicationTestEmailBodyTemplate",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CommunicationTestEmailSubjectTemplate",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CommunicationTestSmsTemplate",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CommunicationTestWhatsAppTemplate",
                schema: "Settings",
                table: "SiteSettings");
        }
    }
}
