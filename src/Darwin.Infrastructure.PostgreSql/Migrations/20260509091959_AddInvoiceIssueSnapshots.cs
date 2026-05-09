using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceIssueSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedAtUtc",
                schema: "CRM",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuedSnapshotJson",
                schema: "CRM",
                table: "Invoices",
                type: "character varying(16000)",
                maxLength: 16000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_IssuedAtUtc",
                schema: "CRM",
                table: "Invoices",
                column: "IssuedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_IssuedAtUtc",
                schema: "CRM",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IssuedAtUtc",
                schema: "CRM",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IssuedSnapshotJson",
                schema: "CRM",
                table: "Invoices");
        }
    }
}
