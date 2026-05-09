using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceArchivePurgeMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchivePurgeReason",
                schema: "CRM",
                table: "Invoices",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivePurgedAtUtc",
                schema: "CRM",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ArchivePurgedAtUtc",
                schema: "CRM",
                table: "Invoices",
                column: "ArchivePurgedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_ArchivePurgedAtUtc",
                schema: "CRM",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ArchivePurgeReason",
                schema: "CRM",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ArchivePurgedAtUtc",
                schema: "CRM",
                table: "Invoices");
        }
    }
}
