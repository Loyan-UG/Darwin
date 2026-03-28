using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SiteSettingStripeAndDhlProviderSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DhlAccountNumber",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DhlApiBaseUrl",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DhlApiKey",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DhlApiSecret",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DhlEnabled",
                schema: "Settings",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DhlEnvironment",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DhlShipperCity",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DhlShipperCountry",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DhlShipperEmail",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DhlShipperName",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DhlShipperPhoneE164",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DhlShipperPostalCode",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DhlShipperStreet",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StripeEnabled",
                schema: "Settings",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StripeMerchantDisplayName",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePublishableKey",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSecretKey",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeWebhookSecret",
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
                name: "DhlAccountNumber",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DhlApiBaseUrl",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DhlApiKey",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DhlApiSecret",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DhlEnabled",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DhlEnvironment",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DhlShipperCity",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DhlShipperCountry",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DhlShipperEmail",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DhlShipperName",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DhlShipperPhoneE164",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DhlShipperPostalCode",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DhlShipperStreet",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "StripeEnabled",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "StripeMerchantDisplayName",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "StripePublishableKey",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "StripeSecretKey",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "StripeWebhookSecret",
                schema: "Settings",
                table: "SiteSettings");
        }
    }
}
