using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class SiteSettingGoogleExternalLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleExternalLoginAndroidClientId",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GoogleExternalLoginEnabled",
                schema: "Settings",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GoogleExternalLoginIosClientId",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleExternalLoginWebClientId",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleExternalLoginAndroidClientId",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "GoogleExternalLoginEnabled",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "GoogleExternalLoginIosClientId",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "GoogleExternalLoginWebClientId",
                schema: "Settings",
                table: "SiteSettings");
        }
    }
}
