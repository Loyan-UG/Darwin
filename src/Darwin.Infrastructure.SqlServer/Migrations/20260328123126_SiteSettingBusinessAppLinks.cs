using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class SiteSettingBusinessAppLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountDeletionUrl",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessManagementWebsiteUrl",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessTermsUrl",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImpressumUrl",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivacyPolicyUrl",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountDeletionUrl",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "BusinessManagementWebsiteUrl",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "BusinessTermsUrl",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ImpressumUrl",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "PrivacyPolicyUrl",
                schema: "Settings",
                table: "SiteSettings");
        }
    }
}
