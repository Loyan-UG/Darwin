using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class SupplierAdvanceReversalHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReversalJournalEntryId",
                schema: "Billing",
                table: "SupplierAdvances",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReversalReason",
                schema: "Billing",
                table: "SupplierAdvances",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReversedAtUtc",
                schema: "Billing",
                table: "SupplierAdvances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReversalJournalEntryId",
                schema: "Billing",
                table: "SupplierAdvanceApplications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReversalReason",
                schema: "Billing",
                table: "SupplierAdvanceApplications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReversedAtUtc",
                schema: "Billing",
                table: "SupplierAdvanceApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAdvances_ReversalJournalEntryId",
                schema: "Billing",
                table: "SupplierAdvances",
                column: "ReversalJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAdvances_ReversedAtUtc",
                schema: "Billing",
                table: "SupplierAdvances",
                column: "ReversedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAdvanceApplications_ReversalJournalEntryId",
                schema: "Billing",
                table: "SupplierAdvanceApplications",
                column: "ReversalJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAdvanceApplications_ReversedAtUtc",
                schema: "Billing",
                table: "SupplierAdvanceApplications",
                column: "ReversedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupplierAdvances_ReversalJournalEntryId",
                schema: "Billing",
                table: "SupplierAdvances");

            migrationBuilder.DropIndex(
                name: "IX_SupplierAdvances_ReversedAtUtc",
                schema: "Billing",
                table: "SupplierAdvances");

            migrationBuilder.DropIndex(
                name: "IX_SupplierAdvanceApplications_ReversalJournalEntryId",
                schema: "Billing",
                table: "SupplierAdvanceApplications");

            migrationBuilder.DropIndex(
                name: "IX_SupplierAdvanceApplications_ReversedAtUtc",
                schema: "Billing",
                table: "SupplierAdvanceApplications");

            migrationBuilder.DropColumn(
                name: "ReversalJournalEntryId",
                schema: "Billing",
                table: "SupplierAdvanceApplications");

            migrationBuilder.DropColumn(
                name: "ReversalReason",
                schema: "Billing",
                table: "SupplierAdvanceApplications");

            migrationBuilder.DropColumn(
                name: "ReversedAtUtc",
                schema: "Billing",
                table: "SupplierAdvanceApplications");

            migrationBuilder.DropColumn(
                name: "ReversalJournalEntryId",
                schema: "Billing",
                table: "SupplierAdvances");

            migrationBuilder.DropColumn(
                name: "ReversalReason",
                schema: "Billing",
                table: "SupplierAdvances");

            migrationBuilder.DropColumn(
                name: "ReversedAtUtc",
                schema: "Billing",
                table: "SupplierAdvances");
        }
    }
}
