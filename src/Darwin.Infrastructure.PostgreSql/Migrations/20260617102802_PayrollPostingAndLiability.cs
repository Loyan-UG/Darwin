using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class PayrollPostingAndLiability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PostedAtUtc",
                schema: "HumanResources",
                table: "PayrollRuns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PostingJournalEntryId",
                schema: "HumanResources",
                table: "PayrollRuns",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_PostedAtUtc",
                schema: "HumanResources",
                table: "PayrollRuns",
                column: "PostedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_PostingJournalEntryId",
                schema: "HumanResources",
                table: "PayrollRuns",
                column: "PostingJournalEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PayrollRuns_PostedAtUtc",
                schema: "HumanResources",
                table: "PayrollRuns");

            migrationBuilder.DropIndex(
                name: "IX_PayrollRuns_PostingJournalEntryId",
                schema: "HumanResources",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "PostedAtUtc",
                schema: "HumanResources",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "PostingJournalEntryId",
                schema: "HumanResources",
                table: "PayrollRuns");
        }
    }
}
