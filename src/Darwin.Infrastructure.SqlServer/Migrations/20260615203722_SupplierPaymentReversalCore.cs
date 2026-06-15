using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class SupplierPaymentReversalCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReversalJournalEntryId",
                schema: "Billing",
                table: "SupplierPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReversalReason",
                schema: "Billing",
                table: "SupplierPayments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReversedAtUtc",
                schema: "Billing",
                table: "SupplierPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_ReversalJournalEntryId",
                schema: "Billing",
                table: "SupplierPayments",
                column: "ReversalJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_ReversedAtUtc",
                schema: "Billing",
                table: "SupplierPayments",
                column: "ReversedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_ReversalJournalEntryId",
                schema: "Billing",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_ReversedAtUtc",
                schema: "Billing",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "ReversalJournalEntryId",
                schema: "Billing",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "ReversalReason",
                schema: "Billing",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "ReversedAtUtc",
                schema: "Billing",
                table: "SupplierPayments");
        }
    }
}
