using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class WarehouseTaskFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarehouseTasks",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaskNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaskType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReadyAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_WarehouseTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseTaskLines",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FromLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SkuSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RequestedQuantity = table.Column<int>(type: "int", nullable: false),
                    CompletedQuantity = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SourceLineType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_WarehouseTaskLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseTaskLines_WarehouseTasks_WarehouseTaskId",
                        column: x => x.WarehouseTaskId,
                        principalSchema: "Inventory",
                        principalTable: "WarehouseTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTaskLines_FromLocationId",
                schema: "Inventory",
                table: "WarehouseTaskLines",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTaskLines_ProductVariantId",
                schema: "Inventory",
                table: "WarehouseTaskLines",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTaskLines_SortOrder",
                schema: "Inventory",
                table: "WarehouseTaskLines",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTaskLines_SourceLineId",
                schema: "Inventory",
                table: "WarehouseTaskLines",
                column: "SourceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTaskLines_ToLocationId",
                schema: "Inventory",
                table: "WarehouseTaskLines",
                column: "ToLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTaskLines_WarehouseTaskId",
                schema: "Inventory",
                table: "WarehouseTaskLines",
                column: "WarehouseTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTasks_AssignedToUserId",
                schema: "Inventory",
                table: "WarehouseTasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTasks_BusinessId",
                schema: "Inventory",
                table: "WarehouseTasks",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTasks_BusinessId_TaskNumber",
                schema: "Inventory",
                table: "WarehouseTasks",
                columns: new[] { "BusinessId", "TaskNumber" },
                unique: true,
                filter: "[TaskNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTasks_CompletedAtUtc",
                schema: "Inventory",
                table: "WarehouseTasks",
                column: "CompletedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTasks_DueAtUtc",
                schema: "Inventory",
                table: "WarehouseTasks",
                column: "DueAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTasks_FromLocationId",
                schema: "Inventory",
                table: "WarehouseTasks",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTasks_Priority",
                schema: "Inventory",
                table: "WarehouseTasks",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTasks_ReadyAtUtc",
                schema: "Inventory",
                table: "WarehouseTasks",
                column: "ReadyAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTasks_SourceEntityId",
                schema: "Inventory",
                table: "WarehouseTasks",
                column: "SourceEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTasks_SourceType",
                schema: "Inventory",
                table: "WarehouseTasks",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTasks_Status",
                schema: "Inventory",
                table: "WarehouseTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTasks_TaskType",
                schema: "Inventory",
                table: "WarehouseTasks",
                column: "TaskType");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTasks_ToLocationId",
                schema: "Inventory",
                table: "WarehouseTasks",
                column: "ToLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTasks_WarehouseId",
                schema: "Inventory",
                table: "WarehouseTasks",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarehouseTaskLines",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "WarehouseTasks",
                schema: "Inventory");
        }
    }
}
