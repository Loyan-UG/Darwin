using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class WarehouseLabelTemplatePrinting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarehouseLabelTemplates",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TemplateKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Format = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    WidthMm = table.Column<int>(type: "int", nullable: false),
                    HeightMm = table.Column<int>(type: "int", nullable: false),
                    ContentTemplate = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_WarehouseLabelTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLabelTemplates_BusinessId",
                schema: "Inventory",
                table: "WarehouseLabelTemplates",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLabelTemplates_BusinessId_IsDefault",
                schema: "Inventory",
                table: "WarehouseLabelTemplates",
                columns: new[] { "BusinessId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLabelTemplates_BusinessId_TemplateKey",
                schema: "Inventory",
                table: "WarehouseLabelTemplates",
                columns: new[] { "BusinessId", "TemplateKey" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLabelTemplates_Format",
                schema: "Inventory",
                table: "WarehouseLabelTemplates",
                column: "Format");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLabelTemplates_Status",
                schema: "Inventory",
                table: "WarehouseLabelTemplates",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarehouseLabelTemplates",
                schema: "Inventory");
        }
    }
}
