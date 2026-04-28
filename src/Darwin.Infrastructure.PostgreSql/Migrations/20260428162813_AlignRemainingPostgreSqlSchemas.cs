using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AlignRemainingPostgreSqlSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "VariantOptionValues",
                newName: "VariantOptionValues",
                newSchema: "Catalog");

            migrationBuilder.RenameTable(
                name: "Shipments",
                newName: "Shipments",
                newSchema: "Orders");

            migrationBuilder.RenameTable(
                name: "ShipmentLines",
                newName: "ShipmentLines",
                newSchema: "Orders");

            migrationBuilder.RenameTable(
                name: "ShipmentCarrierEvents",
                newName: "ShipmentCarrierEvents",
                newSchema: "Shipping");

            migrationBuilder.RenameTable(
                name: "Refunds",
                newName: "Refunds",
                newSchema: "Orders");

            migrationBuilder.RenameTable(
                name: "ProductOptionValues",
                newName: "ProductOptionValues",
                newSchema: "Catalog");

            migrationBuilder.RenameTable(
                name: "ProductMedia",
                newName: "ProductMedia",
                newSchema: "Catalog");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "VariantOptionValues",
                schema: "Catalog",
                newName: "VariantOptionValues");

            migrationBuilder.RenameTable(
                name: "Shipments",
                schema: "Orders",
                newName: "Shipments");

            migrationBuilder.RenameTable(
                name: "ShipmentLines",
                schema: "Orders",
                newName: "ShipmentLines");

            migrationBuilder.RenameTable(
                name: "ShipmentCarrierEvents",
                schema: "Shipping",
                newName: "ShipmentCarrierEvents");

            migrationBuilder.RenameTable(
                name: "Refunds",
                schema: "Orders",
                newName: "Refunds");

            migrationBuilder.RenameTable(
                name: "ProductOptionValues",
                schema: "Catalog",
                newName: "ProductOptionValues");

            migrationBuilder.RenameTable(
                name: "ProductMedia",
                schema: "Catalog",
                newName: "ProductMedia");
        }
    }
}
