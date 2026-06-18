using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class WarehouseLocationBinCoreModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarehouseLocations",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LocationType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Barcode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_WarehouseLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseLocations_WarehouseLocations_ParentLocationId",
                        column: x => x.ParentLocationId,
                        principalSchema: "Inventory",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseLocations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "Inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_Barcode",
                schema: "Inventory",
                table: "WarehouseLocations",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_BusinessId",
                schema: "Inventory",
                table: "WarehouseLocations",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_BusinessId_WarehouseId_Code",
                schema: "Inventory",
                table: "WarehouseLocations",
                columns: new[] { "BusinessId", "WarehouseId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_LocationType",
                schema: "Inventory",
                table: "WarehouseLocations",
                column: "LocationType");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_ParentLocationId",
                schema: "Inventory",
                table: "WarehouseLocations",
                column: "ParentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_SortOrder",
                schema: "Inventory",
                table: "WarehouseLocations",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_Status",
                schema: "Inventory",
                table: "WarehouseLocations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_WarehouseId",
                schema: "Inventory",
                table: "WarehouseLocations",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarehouseLocations",
                schema: "Inventory");
        }
    }
}
