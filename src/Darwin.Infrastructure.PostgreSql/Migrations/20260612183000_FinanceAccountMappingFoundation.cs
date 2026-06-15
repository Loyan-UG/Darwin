using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class FinanceAccountMappingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancePostingAccountMappings",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 4000, nullable: false, defaultValue: "{}"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePostingAccountMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancePostingAccountMappings_FinancialAccounts_FinancialAccountId",
                        column: x => x.FinancialAccountId,
                        principalSchema: "Billing",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePostingAccountMappings_BusinessId",
                schema: "Billing",
                table: "FinancePostingAccountMappings",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancePostingAccountMappings_FinancialAccountId",
                schema: "Billing",
                table: "FinancePostingAccountMappings",
                column: "FinancialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancePostingAccountMappings_Role",
                schema: "Billing",
                table: "FinancePostingAccountMappings",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "UX_FinancePostingAccountMappings_Business_Role_Active",
                schema: "Billing",
                table: "FinancePostingAccountMappings",
                columns: new[] { "BusinessId", "Role" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancePostingAccountMappings",
                schema: "Billing");
        }
    }
}
