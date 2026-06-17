using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class WarehousePickingShortageAttention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShortQuantity",
                schema: "Inventory",
                table: "WarehouseTaskLines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ShortReason",
                schema: "Inventory",
                table: "WarehouseTaskLines",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTaskLines_ShortQuantity",
                schema: "Inventory",
                table: "WarehouseTaskLines",
                column: "ShortQuantity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WarehouseTaskLines_ShortQuantity",
                schema: "Inventory",
                table: "WarehouseTaskLines");

            migrationBuilder.DropColumn(
                name: "ShortQuantity",
                schema: "Inventory",
                table: "WarehouseTaskLines");

            migrationBuilder.DropColumn(
                name: "ShortReason",
                schema: "Inventory",
                table: "WarehouseTaskLines");
        }
    }
}
