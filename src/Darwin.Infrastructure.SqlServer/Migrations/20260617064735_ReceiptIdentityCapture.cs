using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class ReceiptIdentityCapture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoodsReceiptLineIdentities",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InventorySerialUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HandlingUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    LotCodeSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SupplierLotCodeSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SerialNumberSnapshot = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    HandlingUnitCodeSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExpiryDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptLineIdentities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLineIdentities_GoodsReceiptLines_GoodsReceiptLineId",
                        column: x => x.GoodsReceiptLineId,
                        principalSchema: "Inventory",
                        principalTable: "GoodsReceiptLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLineIdentities_ExpiryDateUtc",
                schema: "Inventory",
                table: "GoodsReceiptLineIdentities",
                column: "ExpiryDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLineIdentities_GoodsReceiptLineId",
                schema: "Inventory",
                table: "GoodsReceiptLineIdentities",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLineIdentities_GoodsReceiptLineId_InventorySerialUnitId",
                schema: "Inventory",
                table: "GoodsReceiptLineIdentities",
                columns: new[] { "GoodsReceiptLineId", "InventorySerialUnitId" },
                unique: true,
                filter: "[InventorySerialUnitId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLineIdentities_HandlingUnitId",
                schema: "Inventory",
                table: "GoodsReceiptLineIdentities",
                column: "HandlingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLineIdentities_InventoryLotId",
                schema: "Inventory",
                table: "GoodsReceiptLineIdentities",
                column: "InventoryLotId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLineIdentities_InventorySerialUnitId",
                schema: "Inventory",
                table: "GoodsReceiptLineIdentities",
                column: "InventorySerialUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLineIdentities_ProductVariantId",
                schema: "Inventory",
                table: "GoodsReceiptLineIdentities",
                column: "ProductVariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoodsReceiptLineIdentities",
                schema: "Inventory");
        }
    }
}
