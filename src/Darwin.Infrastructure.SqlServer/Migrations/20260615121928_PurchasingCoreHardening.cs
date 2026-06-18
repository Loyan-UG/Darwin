using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class PurchasingCoreHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "Inventory",
                table: "Suppliers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalNotes",
                schema: "Inventory",
                table: "Suppliers",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LeadTimeDays",
                schema: "Inventory",
                table: "Suppliers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentTermDays",
                schema: "Inventory",
                table: "Suppliers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredCurrency",
                schema: "Inventory",
                table: "Suppliers",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "Status",
                schema: "Inventory",
                table: "Suppliers",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<string>(
                name: "TaxRegistrationNumber",
                schema: "Inventory",
                table: "Suppliers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                schema: "Inventory",
                table: "Suppliers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                schema: "Inventory",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                schema: "Inventory",
                table: "PurchaseOrders",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDeliveryDateUtc",
                schema: "Inventory",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalNotes",
                schema: "Inventory",
                table: "PurchaseOrders",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedAtUtc",
                schema: "Inventory",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedAtUtc",
                schema: "Inventory",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledQuantity",
                schema: "Inventory",
                table: "PurchaseOrderLines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "Inventory",
                table: "PurchaseOrderLines",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceivedQuantity",
                schema: "Inventory",
                table: "PurchaseOrderLines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SupplierSku",
                schema: "Inventory",
                table: "PurchaseOrderLines",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_BusinessId_Code",
                schema: "Inventory",
                table: "Suppliers",
                columns: new[] { "BusinessId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_BusinessId_Status",
                schema: "Inventory",
                table: "Suppliers",
                columns: new[] { "BusinessId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CancelledAtUtc",
                schema: "Inventory",
                table: "PurchaseOrders",
                column: "CancelledAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ExpectedDeliveryDateUtc",
                schema: "Inventory",
                table: "PurchaseOrders",
                column: "ExpectedDeliveryDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_IssuedAtUtc",
                schema: "Inventory",
                table: "PurchaseOrders",
                column: "IssuedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ReceivedAtUtc",
                schema: "Inventory",
                table: "PurchaseOrders",
                column: "ReceivedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status",
                schema: "Inventory",
                table: "PurchaseOrders",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Suppliers_BusinessId_Code", schema: "Inventory", table: "Suppliers");
            migrationBuilder.DropIndex(name: "IX_Suppliers_BusinessId_Status", schema: "Inventory", table: "Suppliers");
            migrationBuilder.DropIndex(name: "IX_PurchaseOrders_CancelledAtUtc", schema: "Inventory", table: "PurchaseOrders");
            migrationBuilder.DropIndex(name: "IX_PurchaseOrders_ExpectedDeliveryDateUtc", schema: "Inventory", table: "PurchaseOrders");
            migrationBuilder.DropIndex(name: "IX_PurchaseOrders_IssuedAtUtc", schema: "Inventory", table: "PurchaseOrders");
            migrationBuilder.DropIndex(name: "IX_PurchaseOrders_ReceivedAtUtc", schema: "Inventory", table: "PurchaseOrders");
            migrationBuilder.DropIndex(name: "IX_PurchaseOrders_Status", schema: "Inventory", table: "PurchaseOrders");

            migrationBuilder.DropColumn(name: "Code", schema: "Inventory", table: "Suppliers");
            migrationBuilder.DropColumn(name: "ExternalNotes", schema: "Inventory", table: "Suppliers");
            migrationBuilder.DropColumn(name: "LeadTimeDays", schema: "Inventory", table: "Suppliers");
            migrationBuilder.DropColumn(name: "PaymentTermDays", schema: "Inventory", table: "Suppliers");
            migrationBuilder.DropColumn(name: "PreferredCurrency", schema: "Inventory", table: "Suppliers");
            migrationBuilder.DropColumn(name: "Status", schema: "Inventory", table: "Suppliers");
            migrationBuilder.DropColumn(name: "TaxRegistrationNumber", schema: "Inventory", table: "Suppliers");
            migrationBuilder.DropColumn(name: "Website", schema: "Inventory", table: "Suppliers");
            migrationBuilder.DropColumn(name: "CancelledAtUtc", schema: "Inventory", table: "PurchaseOrders");
            migrationBuilder.DropColumn(name: "Currency", schema: "Inventory", table: "PurchaseOrders");
            migrationBuilder.DropColumn(name: "ExpectedDeliveryDateUtc", schema: "Inventory", table: "PurchaseOrders");
            migrationBuilder.DropColumn(name: "InternalNotes", schema: "Inventory", table: "PurchaseOrders");
            migrationBuilder.DropColumn(name: "IssuedAtUtc", schema: "Inventory", table: "PurchaseOrders");
            migrationBuilder.DropColumn(name: "ReceivedAtUtc", schema: "Inventory", table: "PurchaseOrders");
            migrationBuilder.DropColumn(name: "CancelledQuantity", schema: "Inventory", table: "PurchaseOrderLines");
            migrationBuilder.DropColumn(name: "Description", schema: "Inventory", table: "PurchaseOrderLines");
            migrationBuilder.DropColumn(name: "ReceivedQuantity", schema: "Inventory", table: "PurchaseOrderLines");
            migrationBuilder.DropColumn(name: "SupplierSku", schema: "Inventory", table: "PurchaseOrderLines");
        }
    }
}
