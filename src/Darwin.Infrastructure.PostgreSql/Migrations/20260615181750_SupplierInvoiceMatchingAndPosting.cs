using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class SupplierInvoiceMatchingAndPosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PostedAtUtc",
                schema: "Billing",
                table: "SupplierInvoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PostingJournalEntryId",
                schema: "Billing",
                table: "SupplierInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_PostedAtUtc",
                schema: "Billing",
                table: "SupplierInvoices",
                column: "PostedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_PostingJournalEntryId",
                schema: "Billing",
                table: "SupplierInvoices",
                column: "PostingJournalEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupplierInvoices_PostedAtUtc",
                schema: "Billing",
                table: "SupplierInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SupplierInvoices_PostingJournalEntryId",
                schema: "Billing",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "PostedAtUtc",
                schema: "Billing",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "PostingJournalEntryId",
                schema: "Billing",
                table: "SupplierInvoices");
        }
    }
}
