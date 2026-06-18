using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class StockCountCoreModelInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockCountSessions",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CountNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CountType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CountWindowStartUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CountWindowEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreparedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CountedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewRequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_StockCountSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockCountLines",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockCountSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SkuSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ExpectedQuantity = table.Column<int>(type: "int", nullable: false),
                    CountedQuantity = table.Column<int>(type: "int", nullable: false),
                    VarianceQuantity = table.Column<int>(type: "int", nullable: false),
                    ReviewStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AdjustmentPosted = table.Column<bool>(type: "bit", nullable: false),
                    ReviewNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_StockCountLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockCountLines_StockCountSessions_StockCountSessionId",
                        column: x => x.StockCountSessionId,
                        principalSchema: "Inventory",
                        principalTable: "StockCountSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_AdjustmentPosted",
                schema: "Inventory",
                table: "StockCountLines",
                column: "AdjustmentPosted");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_LocationId",
                schema: "Inventory",
                table: "StockCountLines",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_ProductVariantId",
                schema: "Inventory",
                table: "StockCountLines",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_ReviewStatus",
                schema: "Inventory",
                table: "StockCountLines",
                column: "ReviewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_SortOrder",
                schema: "Inventory",
                table: "StockCountLines",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_StockCountSessionId",
                schema: "Inventory",
                table: "StockCountLines",
                column: "StockCountSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_StockCountSessionId_ProductVariantId",
                schema: "Inventory",
                table: "StockCountLines",
                columns: new[] { "StockCountSessionId", "ProductVariantId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountSessions_AssignedToUserId",
                schema: "Inventory",
                table: "StockCountSessions",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountSessions_BusinessId",
                schema: "Inventory",
                table: "StockCountSessions",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountSessions_BusinessId_CountNumber",
                schema: "Inventory",
                table: "StockCountSessions",
                columns: new[] { "BusinessId", "CountNumber" },
                unique: true,
                filter: "[CountNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountSessions_CountType",
                schema: "Inventory",
                table: "StockCountSessions",
                column: "CountType");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountSessions_CountWindowStartUtc",
                schema: "Inventory",
                table: "StockCountSessions",
                column: "CountWindowStartUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountSessions_LocationId",
                schema: "Inventory",
                table: "StockCountSessions",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountSessions_PostedAtUtc",
                schema: "Inventory",
                table: "StockCountSessions",
                column: "PostedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountSessions_PreparedAtUtc",
                schema: "Inventory",
                table: "StockCountSessions",
                column: "PreparedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountSessions_Status",
                schema: "Inventory",
                table: "StockCountSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountSessions_WarehouseId",
                schema: "Inventory",
                table: "StockCountSessions",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockCountLines",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "StockCountSessions",
                schema: "Inventory");
        }
    }
}
