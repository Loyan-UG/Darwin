using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceArchiveRetentionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InvoiceArchiveRetentionYears",
                schema: "Settings",
                table: "SiteSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchiveGeneratedAtUtc",
                schema: "CRM",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchiveRetainUntilUtc",
                schema: "CRM",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchiveRetentionPolicyVersion",
                schema: "CRM",
                table: "Invoices",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuedSnapshotHashSha256",
                schema: "CRM",
                table: "Invoices",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ArchiveRetainUntilUtc",
                schema: "CRM",
                table: "Invoices",
                column: "ArchiveRetainUntilUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_ArchiveRetainUntilUtc",
                schema: "CRM",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "InvoiceArchiveRetentionYears",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ArchiveGeneratedAtUtc",
                schema: "CRM",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ArchiveRetainUntilUtc",
                schema: "CRM",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ArchiveRetentionPolicyVersion",
                schema: "CRM",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IssuedSnapshotHashSha256",
                schema: "CRM",
                table: "Invoices");
        }
    }
}
