using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class PayrollPaymentReversalCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReversalJournalEntryId",
                schema: "HumanResources",
                table: "PayrollPayments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReversalReason",
                schema: "HumanResources",
                table: "PayrollPayments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReversedAtUtc",
                schema: "HumanResources",
                table: "PayrollPayments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_ReversalJournalEntryId",
                schema: "HumanResources",
                table: "PayrollPayments",
                column: "ReversalJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_ReversedAtUtc",
                schema: "HumanResources",
                table: "PayrollPayments",
                column: "ReversedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PayrollPayments_ReversalJournalEntryId",
                schema: "HumanResources",
                table: "PayrollPayments");

            migrationBuilder.DropIndex(
                name: "IX_PayrollPayments_ReversedAtUtc",
                schema: "HumanResources",
                table: "PayrollPayments");

            migrationBuilder.DropColumn(
                name: "ReversalJournalEntryId",
                schema: "HumanResources",
                table: "PayrollPayments");

            migrationBuilder.DropColumn(
                name: "ReversalReason",
                schema: "HumanResources",
                table: "PayrollPayments");

            migrationBuilder.DropColumn(
                name: "ReversedAtUtc",
                schema: "HumanResources",
                table: "PayrollPayments");
        }
    }
}
