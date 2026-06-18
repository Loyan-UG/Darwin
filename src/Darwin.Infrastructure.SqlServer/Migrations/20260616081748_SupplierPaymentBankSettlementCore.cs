using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class SupplierPaymentBankSettlementCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BankSettledAtUtc",
                schema: "Billing",
                table: "SupplierPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BankSettlementJournalEntryId",
                schema: "Billing",
                table: "SupplierPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankSettlementNotes",
                schema: "Billing",
                table: "SupplierPayments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BankSettlementReconciliationMatchId",
                schema: "Billing",
                table: "SupplierPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_BankSettledAtUtc",
                schema: "Billing",
                table: "SupplierPayments",
                column: "BankSettledAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_BankSettlementJournalEntryId",
                schema: "Billing",
                table: "SupplierPayments",
                column: "BankSettlementJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_BankSettlementReconciliationMatchId",
                schema: "Billing",
                table: "SupplierPayments",
                column: "BankSettlementReconciliationMatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_BankSettledAtUtc",
                schema: "Billing",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_BankSettlementJournalEntryId",
                schema: "Billing",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_BankSettlementReconciliationMatchId",
                schema: "Billing",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "BankSettledAtUtc",
                schema: "Billing",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "BankSettlementJournalEntryId",
                schema: "Billing",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "BankSettlementNotes",
                schema: "Billing",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "BankSettlementReconciliationMatchId",
                schema: "Billing",
                table: "SupplierPayments");
        }
    }
}
