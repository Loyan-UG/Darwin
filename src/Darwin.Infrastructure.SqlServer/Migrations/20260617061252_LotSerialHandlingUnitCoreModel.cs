using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class LotSerialHandlingUnitCoreModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HandlingUnits",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentHandlingUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    HandlingUnitType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_HandlingUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandlingUnits_HandlingUnits_ParentHandlingUnitId",
                        column: x => x.ParentHandlingUnitId,
                        principalSchema: "Inventory",
                        principalTable: "HandlingUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryLots",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LotCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SupplierLotCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ManufactureDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_InventoryLots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventorySerialUnits",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ManufactureDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_InventorySerialUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductTrackingPolicies",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackingMode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequiresSupplierLot = table.Column<bool>(type: "bit", nullable: false),
                    RequiresExpiryDate = table.Column<bool>(type: "bit", nullable: false),
                    RequiresHandlingUnit = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_ProductTrackingPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HandlingUnitContents",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandlingUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InventorySerialUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    SkuSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_HandlingUnitContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandlingUnitContents_HandlingUnits_HandlingUnitId",
                        column: x => x.HandlingUnitId,
                        principalSchema: "Inventory",
                        principalTable: "HandlingUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnitContents_HandlingUnitId",
                schema: "Inventory",
                table: "HandlingUnitContents",
                column: "HandlingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnitContents_InventoryLotId",
                schema: "Inventory",
                table: "HandlingUnitContents",
                column: "InventoryLotId");

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnitContents_InventorySerialUnitId",
                schema: "Inventory",
                table: "HandlingUnitContents",
                column: "InventorySerialUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnitContents_ProductVariantId",
                schema: "Inventory",
                table: "HandlingUnitContents",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnitContents_SortOrder",
                schema: "Inventory",
                table: "HandlingUnitContents",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnits_Barcode",
                schema: "Inventory",
                table: "HandlingUnits",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnits_BusinessId",
                schema: "Inventory",
                table: "HandlingUnits",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnits_BusinessId_Code",
                schema: "Inventory",
                table: "HandlingUnits",
                columns: new[] { "BusinessId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnits_HandlingUnitType",
                schema: "Inventory",
                table: "HandlingUnits",
                column: "HandlingUnitType");

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnits_LocationId",
                schema: "Inventory",
                table: "HandlingUnits",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnits_ParentHandlingUnitId",
                schema: "Inventory",
                table: "HandlingUnits",
                column: "ParentHandlingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnits_Status",
                schema: "Inventory",
                table: "HandlingUnits",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnits_WarehouseId",
                schema: "Inventory",
                table: "HandlingUnits",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_BusinessId",
                schema: "Inventory",
                table: "InventoryLots",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_BusinessId_ProductVariantId_LotCode",
                schema: "Inventory",
                table: "InventoryLots",
                columns: new[] { "BusinessId", "ProductVariantId", "LotCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_ExpiryDateUtc",
                schema: "Inventory",
                table: "InventoryLots",
                column: "ExpiryDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_ProductVariantId",
                schema: "Inventory",
                table: "InventoryLots",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_Status",
                schema: "Inventory",
                table: "InventoryLots",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_SupplierLotCode",
                schema: "Inventory",
                table: "InventoryLots",
                column: "SupplierLotCode");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySerialUnits_BusinessId",
                schema: "Inventory",
                table: "InventorySerialUnits",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySerialUnits_BusinessId_ProductVariantId_SerialNumber",
                schema: "Inventory",
                table: "InventorySerialUnits",
                columns: new[] { "BusinessId", "ProductVariantId", "SerialNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySerialUnits_ExpiryDateUtc",
                schema: "Inventory",
                table: "InventorySerialUnits",
                column: "ExpiryDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySerialUnits_InventoryLotId",
                schema: "Inventory",
                table: "InventorySerialUnits",
                column: "InventoryLotId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySerialUnits_ProductVariantId",
                schema: "Inventory",
                table: "InventorySerialUnits",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySerialUnits_Status",
                schema: "Inventory",
                table: "InventorySerialUnits",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTrackingPolicies_BusinessId",
                schema: "Inventory",
                table: "ProductTrackingPolicies",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTrackingPolicies_BusinessId_ProductVariantId",
                schema: "Inventory",
                table: "ProductTrackingPolicies",
                columns: new[] { "BusinessId", "ProductVariantId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTrackingPolicies_ProductVariantId",
                schema: "Inventory",
                table: "ProductTrackingPolicies",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTrackingPolicies_Status",
                schema: "Inventory",
                table: "ProductTrackingPolicies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTrackingPolicies_TrackingMode",
                schema: "Inventory",
                table: "ProductTrackingPolicies",
                column: "TrackingMode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HandlingUnitContents",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "InventoryLots",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "InventorySerialUnits",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "ProductTrackingPolicies",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "HandlingUnits",
                schema: "Inventory");
        }
    }
}
