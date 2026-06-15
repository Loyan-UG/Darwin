using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
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
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReversalReason",
                schema: "Billing",
                table: "SupplierPayments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReversedAtUtc",
                schema: "Billing",
                table: "SupplierPayments",
                type: "timestamp with time zone",
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
