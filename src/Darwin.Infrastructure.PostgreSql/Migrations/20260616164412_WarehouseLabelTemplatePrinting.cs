using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TemplateKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Format = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    WidthMm = table.Column<int>(type: "integer", nullable: false),
                    HeightMm = table.Column<int>(type: "integer", nullable: false),
                    ContentTemplate = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
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
                filter: "\"IsDeleted\" = FALSE");

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
