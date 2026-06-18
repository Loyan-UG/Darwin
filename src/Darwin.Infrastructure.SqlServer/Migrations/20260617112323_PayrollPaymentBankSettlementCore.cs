using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class PayrollPaymentBankSettlementCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BankSettledAtUtc",
                schema: "HumanResources",
                table: "PayrollPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BankSettlementJournalEntryId",
                schema: "HumanResources",
                table: "PayrollPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankSettlementNotes",
                schema: "HumanResources",
                table: "PayrollPayments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BankSettlementReconciliationMatchId",
                schema: "HumanResources",
                table: "PayrollPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_BankSettledAtUtc",
                schema: "HumanResources",
                table: "PayrollPayments",
                column: "BankSettledAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_BankSettlementJournalEntryId",
                schema: "HumanResources",
                table: "PayrollPayments",
                column: "BankSettlementJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_BankSettlementReconciliationMatchId",
                schema: "HumanResources",
                table: "PayrollPayments",
                column: "BankSettlementReconciliationMatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PayrollPayments_BankSettledAtUtc",
                schema: "HumanResources",
                table: "PayrollPayments");

            migrationBuilder.DropIndex(
                name: "IX_PayrollPayments_BankSettlementJournalEntryId",
                schema: "HumanResources",
                table: "PayrollPayments");

            migrationBuilder.DropIndex(
                name: "IX_PayrollPayments_BankSettlementReconciliationMatchId",
                schema: "HumanResources",
                table: "PayrollPayments");

            migrationBuilder.DropColumn(
                name: "BankSettledAtUtc",
                schema: "HumanResources",
                table: "PayrollPayments");

            migrationBuilder.DropColumn(
                name: "BankSettlementJournalEntryId",
                schema: "HumanResources",
                table: "PayrollPayments");

            migrationBuilder.DropColumn(
                name: "BankSettlementNotes",
                schema: "HumanResources",
                table: "PayrollPayments");

            migrationBuilder.DropColumn(
                name: "BankSettlementReconciliationMatchId",
                schema: "HumanResources",
                table: "PayrollPayments");
        }
    }
}
