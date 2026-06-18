using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class TransferCountPickIdentityIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockCountLineIdentities",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockCountLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventorySerialUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    HandlingUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    LotCodeSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SupplierLotCodeSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiryDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SerialNumberSnapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    HandlingUnitCodeSnapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 8000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCountLineIdentities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockCountLineIdentities_StockCountLines_StockCountLineId",
                        column: x => x.StockCountLineId,
                        principalSchema: "Inventory",
                        principalTable: "StockCountLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferLineIdentities",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockTransferLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventorySerialUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    HandlingUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    LotCodeSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SupplierLotCodeSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiryDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SerialNumberSnapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    HandlingUnitCodeSnapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 8000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferLineIdentities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferLineIdentities_StockTransferLines_StockTransfe~",
                        column: x => x.StockTransferLineId,
                        principalSchema: "Inventory",
                        principalTable: "StockTransferLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseTaskLineIdentities",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseTaskLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventorySerialUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    HandlingUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    LotCodeSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SupplierLotCodeSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiryDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SerialNumberSnapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    HandlingUnitCodeSnapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 8000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTaskLineIdentities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseTaskLineIdentities_WarehouseTaskLines_WarehouseTas~",
                        column: x => x.WarehouseTaskLineId,
                        principalSchema: "Inventory",
                        principalTable: "WarehouseTaskLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLineIdentities_HandlingUnitId",
                schema: "Inventory",
                table: "StockCountLineIdentities",
                column: "HandlingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLineIdentities_InventoryLotId",
                schema: "Inventory",
                table: "StockCountLineIdentities",
                column: "InventoryLotId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLineIdentities_InventorySerialUnitId",
                schema: "Inventory",
                table: "StockCountLineIdentities",
                column: "InventorySerialUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLineIdentities_SortOrder",
                schema: "Inventory",
                table: "StockCountLineIdentities",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLineIdentities_StockCountLineId",
                schema: "Inventory",
                table: "StockCountLineIdentities",
                column: "StockCountLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLineIdentities_HandlingUnitId",
                schema: "Inventory",
                table: "StockTransferLineIdentities",
                column: "HandlingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLineIdentities_InventoryLotId",
                schema: "Inventory",
                table: "StockTransferLineIdentities",
                column: "InventoryLotId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLineIdentities_InventorySerialUnitId",
                schema: "Inventory",
                table: "StockTransferLineIdentities",
                column: "InventorySerialUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLineIdentities_SortOrder",
                schema: "Inventory",
                table: "StockTransferLineIdentities",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLineIdentities_StockTransferLineId",
                schema: "Inventory",
                table: "StockTransferLineIdentities",
                column: "StockTransferLineId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTaskLineIdentities_HandlingUnitId",
                schema: "Inventory",
                table: "WarehouseTaskLineIdentities",
                column: "HandlingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTaskLineIdentities_InventoryLotId",
                schema: "Inventory",
                table: "WarehouseTaskLineIdentities",
                column: "InventoryLotId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTaskLineIdentities_InventorySerialUnitId",
                schema: "Inventory",
                table: "WarehouseTaskLineIdentities",
                column: "InventorySerialUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTaskLineIdentities_SortOrder",
                schema: "Inventory",
                table: "WarehouseTaskLineIdentities",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTaskLineIdentities_WarehouseTaskLineId",
                schema: "Inventory",
                table: "WarehouseTaskLineIdentities",
                column: "WarehouseTaskLineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockCountLineIdentities",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "StockTransferLineIdentities",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "WarehouseTaskLineIdentities",
                schema: "Inventory");
        }
    }
}
