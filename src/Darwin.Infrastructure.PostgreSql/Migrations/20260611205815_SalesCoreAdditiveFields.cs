using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class SalesCoreAdditiveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BusinessId",
                schema: "Orders",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                schema: "Orders",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderedAtUtc",
                schema: "Orders",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalesChannel",
                schema: "Orders",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                schema: "CRM",
                table: "Invoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TotalTaxMinor",
                schema: "CRM",
                table: "InvoiceLines",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql("""
                UPDATE "Orders"."Orders"
                SET "OrderedAtUtc" = COALESCE("CreatedAtUtc", NOW())
                WHERE "OrderedAtUtc" IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "OrderedAtUtc",
                schema: "Orders",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.Sql("""
                UPDATE "CRM"."InvoiceLines"
                SET "TotalTaxMinor" = GREATEST(0, "TotalGrossMinor" - "TotalNetMinor")
                WHERE "TotalTaxMinor" = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BusinessId",
                schema: "Orders",
                table: "Orders",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                schema: "Orders",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderedAtUtc",
                schema: "Orders",
                table: "Orders",
                column: "OrderedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SalesChannel",
                schema: "Orders",
                table: "Orders",
                column: "SalesChannel");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                schema: "CRM",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true,
                filter: "\"InvoiceNumber\" IS NOT NULL AND \"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_BusinessId",
                schema: "Orders",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerId",
                schema: "Orders",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderedAtUtc",
                schema: "Orders",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SalesChannel",
                schema: "Orders",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_InvoiceNumber",
                schema: "CRM",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                schema: "Orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                schema: "Orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderedAtUtc",
                schema: "Orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SalesChannel",
                schema: "Orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                schema: "CRM",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TotalTaxMinor",
                schema: "CRM",
                table: "InvoiceLines");
        }
    }
}
