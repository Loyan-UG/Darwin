using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceReverseChargeDecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReverseChargeApplied",
                schema: "CRM",
                table: "Invoices",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReverseChargeReviewNote",
                schema: "CRM",
                table: "Invoices",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReverseChargeReviewedAtUtc",
                schema: "CRM",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ReverseChargeApplied",
                schema: "CRM",
                table: "Invoices",
                column: "ReverseChargeApplied");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_ReverseChargeApplied",
                schema: "CRM",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ReverseChargeApplied",
                schema: "CRM",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ReverseChargeReviewNote",
                schema: "CRM",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ReverseChargeReviewedAtUtc",
                schema: "CRM",
                table: "Invoices");
        }
    }
}
