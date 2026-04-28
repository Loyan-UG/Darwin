using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class SiteSettingPhoneVerificationTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneVerificationSmsTemplate",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneVerificationWhatsAppTemplate",
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
                name: "PhoneVerificationSmsTemplate",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "PhoneVerificationWhatsAppTemplate",
                schema: "Settings",
                table: "SiteSettings");
        }
    }
}
