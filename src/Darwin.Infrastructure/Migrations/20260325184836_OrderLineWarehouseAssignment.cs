using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrderLineWarehouseAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                schema: "Orders",
                table: "OrderLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_WarehouseId",
                schema: "Orders",
                table: "OrderLines",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderLines_Warehouses_WarehouseId",
                schema: "Orders",
                table: "OrderLines",
                column: "WarehouseId",
                principalSchema: "Inventory",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderLines_Warehouses_WarehouseId",
                schema: "Orders",
                table: "OrderLines");

            migrationBuilder.DropIndex(
                name: "IX_OrderLines_WarehouseId",
                schema: "Orders",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                schema: "Orders",
                table: "OrderLines");
        }
    }
}
