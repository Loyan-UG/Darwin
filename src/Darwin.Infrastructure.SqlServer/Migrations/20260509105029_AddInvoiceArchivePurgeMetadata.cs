using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
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
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivePurgedAtUtc",
                schema: "CRM",
                table: "Invoices",
                type: "datetime2",
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
