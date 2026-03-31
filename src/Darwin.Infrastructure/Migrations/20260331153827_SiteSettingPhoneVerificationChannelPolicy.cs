using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SiteSettingPhoneVerificationChannelPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PhoneVerificationAllowFallback",
                schema: "Settings",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PhoneVerificationPreferredChannel",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneVerificationAllowFallback",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "PhoneVerificationPreferredChannel",
                schema: "Settings",
                table: "SiteSettings");
        }
    }
}
