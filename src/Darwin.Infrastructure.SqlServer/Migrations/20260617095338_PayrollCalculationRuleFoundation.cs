using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class PayrollCalculationRuleFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollRuleSets",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JurisdictionCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RuleSetCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RuleVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
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
                    table.PrimaryKey("PK_PayrollRuleSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRuleComponents",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollRuleSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ComponentType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CalculationMethod = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Basis = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RateBasisPoints = table.Column<int>(type: "int", nullable: true),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: true),
                    ThresholdJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    IsEmployerCost = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PayrollRuleComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRuleComponents_PayrollRuleSets_PayrollRuleSetId",
                        column: x => x.PayrollRuleSetId,
                        principalSchema: "HumanResources",
                        principalTable: "PayrollRuleSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuleComponents_BusinessId",
                schema: "HumanResources",
                table: "PayrollRuleComponents",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuleComponents_ComponentType",
                schema: "HumanResources",
                table: "PayrollRuleComponents",
                column: "ComponentType");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuleComponents_PayrollRuleSetId",
                schema: "HumanResources",
                table: "PayrollRuleComponents",
                column: "PayrollRuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuleComponents_PayrollRuleSetId_ComponentCode",
                schema: "HumanResources",
                table: "PayrollRuleComponents",
                columns: new[] { "PayrollRuleSetId", "ComponentCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuleComponents_SortOrder",
                schema: "HumanResources",
                table: "PayrollRuleComponents",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuleSets_BusinessId",
                schema: "HumanResources",
                table: "PayrollRuleSets",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuleSets_BusinessId_JurisdictionCode_RuleSetCode_RuleVersion",
                schema: "HumanResources",
                table: "PayrollRuleSets",
                columns: new[] { "BusinessId", "JurisdictionCode", "RuleSetCode", "RuleVersion" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuleSets_EffectiveFromUtc",
                schema: "HumanResources",
                table: "PayrollRuleSets",
                column: "EffectiveFromUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuleSets_EffectiveToUtc",
                schema: "HumanResources",
                table: "PayrollRuleSets",
                column: "EffectiveToUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuleSets_JurisdictionCode",
                schema: "HumanResources",
                table: "PayrollRuleSets",
                column: "JurisdictionCode");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuleSets_RuleSetCode",
                schema: "HumanResources",
                table: "PayrollRuleSets",
                column: "RuleSetCode");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuleSets_Status",
                schema: "HumanResources",
                table: "PayrollRuleSets",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollRuleComponents",
                schema: "HumanResources");

            migrationBuilder.DropTable(
                name: "PayrollRuleSets",
                schema: "HumanResources");
        }
    }
}
