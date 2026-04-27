using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOnTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AddOnGroupTranslations",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddOnGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Culture = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOnGroupTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AddOnGroupTranslations_AddOnGroups_AddOnGroupId",
                        column: x => x.AddOnGroupId,
                        principalSchema: "Catalog",
                        principalTable: "AddOnGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AddOnOptionTranslations",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddOnOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Culture = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOnOptionTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AddOnOptionTranslations_AddOnOptions_AddOnOptionId",
                        column: x => x.AddOnOptionId,
                        principalSchema: "Catalog",
                        principalTable: "AddOnOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AddOnOptionValueTranslations",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddOnOptionValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Culture = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOnOptionValueTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AddOnOptionValueTranslations_AddOnOptionValues_AddOnOptionValueId",
                        column: x => x.AddOnOptionValueId,
                        principalSchema: "Catalog",
                        principalTable: "AddOnOptionValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroupTranslations_AddOnGroupId_Culture",
                schema: "Catalog",
                table: "AddOnGroupTranslations",
                columns: new[] { "AddOnGroupId", "Culture" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AddOnOptionTranslations_AddOnOptionId_Culture",
                schema: "Catalog",
                table: "AddOnOptionTranslations",
                columns: new[] { "AddOnOptionId", "Culture" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AddOnOptionValueTranslations_AddOnOptionValueId_Culture",
                schema: "Catalog",
                table: "AddOnOptionValueTranslations",
                columns: new[] { "AddOnOptionValueId", "Culture" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO [Catalog].[AddOnGroupTranslations] ([Id], [AddOnGroupId], [Culture], [Name], [CreatedAtUtc], [ModifiedAtUtc], [CreatedByUserId], [ModifiedByUserId], [IsDeleted])
                SELECT NEWID(), g.[Id], c.[Culture], g.[Name], SYSUTCDATETIME(), NULL, '00000000-0000-0000-0000-000000000000', '00000000-0000-0000-0000-000000000000', 0
                FROM [Catalog].[AddOnGroups] g
                CROSS JOIN (VALUES ('de-DE'), ('en-US')) c([Culture])
                WHERE g.[IsDeleted] = 0
                  AND NOT EXISTS (
                      SELECT 1 FROM [Catalog].[AddOnGroupTranslations] t
                      WHERE t.[AddOnGroupId] = g.[Id] AND t.[Culture] = c.[Culture]
                  );

                INSERT INTO [Catalog].[AddOnOptionTranslations] ([Id], [AddOnOptionId], [Culture], [Label], [CreatedAtUtc], [ModifiedAtUtc], [CreatedByUserId], [ModifiedByUserId], [IsDeleted])
                SELECT NEWID(), o.[Id], c.[Culture], o.[Label], SYSUTCDATETIME(), NULL, '00000000-0000-0000-0000-000000000000', '00000000-0000-0000-0000-000000000000', 0
                FROM [Catalog].[AddOnOptions] o
                CROSS JOIN (VALUES ('de-DE'), ('en-US')) c([Culture])
                WHERE o.[IsDeleted] = 0
                  AND NOT EXISTS (
                      SELECT 1 FROM [Catalog].[AddOnOptionTranslations] t
                      WHERE t.[AddOnOptionId] = o.[Id] AND t.[Culture] = c.[Culture]
                  );

                INSERT INTO [Catalog].[AddOnOptionValueTranslations] ([Id], [AddOnOptionValueId], [Culture], [Label], [CreatedAtUtc], [ModifiedAtUtc], [CreatedByUserId], [ModifiedByUserId], [IsDeleted])
                SELECT NEWID(), v.[Id], c.[Culture], v.[Label], SYSUTCDATETIME(), NULL, '00000000-0000-0000-0000-000000000000', '00000000-0000-0000-0000-000000000000', 0
                FROM [Catalog].[AddOnOptionValues] v
                CROSS JOIN (VALUES ('de-DE'), ('en-US')) c([Culture])
                WHERE v.[IsDeleted] = 0
                  AND NOT EXISTS (
                      SELECT 1 FROM [Catalog].[AddOnOptionValueTranslations] t
                      WHERE t.[AddOnOptionValueId] = v.[Id] AND t.[Culture] = c.[Culture]
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AddOnGroupTranslations",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "AddOnOptionTranslations",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "AddOnOptionValueTranslations",
                schema: "Catalog");
        }
    }
}
