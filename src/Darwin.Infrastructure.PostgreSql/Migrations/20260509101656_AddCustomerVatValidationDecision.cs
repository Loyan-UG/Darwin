using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerVatValidationDecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "VatValidationCheckedAtUtc",
                schema: "CRM",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatValidationMessage",
                schema: "CRM",
                table: "Customers",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatValidationSource",
                schema: "CRM",
                table: "Customers",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VatValidationStatus",
                schema: "CRM",
                table: "Customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_VatValidationStatus",
                schema: "CRM",
                table: "Customers",
                column: "VatValidationStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_VatValidationStatus",
                schema: "CRM",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "VatValidationCheckedAtUtc",
                schema: "CRM",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "VatValidationMessage",
                schema: "CRM",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "VatValidationSource",
                schema: "CRM",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "VatValidationStatus",
                schema: "CRM",
                table: "Customers");
        }
    }
}
