using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrderShippingMethodSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShippingCarrier",
                schema: "Orders",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ShippingMethodId",
                schema: "Orders",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingMethodName",
                schema: "Orders",
                table: "Orders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingService",
                schema: "Orders",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShippingMethodId",
                schema: "Orders",
                table: "Orders",
                column: "ShippingMethodId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ShippingMethods_ShippingMethodId",
                schema: "Orders",
                table: "Orders",
                column: "ShippingMethodId",
                principalSchema: "Shipping",
                principalTable: "ShippingMethods",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ShippingMethods_ShippingMethodId",
                schema: "Orders",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ShippingMethodId",
                schema: "Orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingCarrier",
                schema: "Orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingMethodId",
                schema: "Orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingMethodName",
                schema: "Orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingService",
                schema: "Orders",
                table: "Orders");
        }
    }
}
