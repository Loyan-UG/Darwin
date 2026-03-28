using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SiteSettingVatAndInvoiceIssuerDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowReverseCharge",
                schema: "Settings",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultVatRatePercent",
                schema: "Settings",
                table: "SiteSettings",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceIssuerAddressLine1",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceIssuerCity",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceIssuerCountry",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceIssuerLegalName",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceIssuerPostalCode",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceIssuerTaxId",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PricesIncludeVat",
                schema: "Settings",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VatEnabled",
                schema: "Settings",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowReverseCharge",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DefaultVatRatePercent",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "InvoiceIssuerAddressLine1",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "InvoiceIssuerCity",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "InvoiceIssuerCountry",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "InvoiceIssuerLegalName",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "InvoiceIssuerPostalCode",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "InvoiceIssuerTaxId",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "PricesIncludeVat",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "VatEnabled",
                schema: "Settings",
                table: "SiteSettings");
        }
    }
}
