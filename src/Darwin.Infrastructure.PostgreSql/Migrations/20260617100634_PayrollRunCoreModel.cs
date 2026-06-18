using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class PayrollRunCoreModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollRuns",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRuleSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    JurisdictionCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RuleSetCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RuleVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmployeeCount = table.Column<int>(type: "integer", nullable: false),
                    GrossPayMinor = table.Column<long>(type: "bigint", nullable: false),
                    EmployeeDeductionMinor = table.Column<long>(type: "bigint", nullable: false),
                    EmployerCostMinor = table.Column<long>(type: "bigint", nullable: false),
                    NetPayMinor = table.Column<long>(type: "bigint", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SourceSnapshotJson = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: true),
                    MetadataJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_PayrollPeriods_PayrollPeriodId",
                        column: x => x.PayrollPeriodId,
                        principalSchema: "HumanResources",
                        principalTable: "PayrollPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_PayrollRuleSets_PayrollRuleSetId",
                        column: x => x.PayrollRuleSetId,
                        principalSchema: "HumanResources",
                        principalTable: "PayrollRuleSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRunLines",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmploymentContractId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployeeNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EmployeeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WorkMinutes = table.Column<int>(type: "integer", nullable: false),
                    BreakMinutes = table.Column<int>(type: "integer", nullable: false),
                    AbsenceMinutes = table.Column<int>(type: "integer", nullable: false),
                    GrossPayMinor = table.Column<long>(type: "bigint", nullable: false),
                    EmployeeDeductionMinor = table.Column<long>(type: "bigint", nullable: false),
                    EmployerCostMinor = table.Column<long>(type: "bigint", nullable: false),
                    NetPayMinor = table.Column<long>(type: "bigint", nullable: false),
                    EmployeeSnapshotJson = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: true),
                    ContractSnapshotJson = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: true),
                    PeriodLineSnapshotJson = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRunLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRunLines_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "HumanResources",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRunLines_EmploymentContracts_EmploymentContractId",
                        column: x => x.EmploymentContractId,
                        principalSchema: "HumanResources",
                        principalTable: "EmploymentContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRunLines_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalSchema: "HumanResources",
                        principalTable: "PayrollRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRunLineComponents",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRunLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRuleComponentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ComponentType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CalculationMethod = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Basis = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    IsEmployerCost = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    RuleSnapshotJson = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRunLineComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRunLineComponents_PayrollRunLines_PayrollRunLineId",
                        column: x => x.PayrollRunLineId,
                        principalSchema: "HumanResources",
                        principalTable: "PayrollRunLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunLineComponents_BusinessId",
                schema: "HumanResources",
                table: "PayrollRunLineComponents",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunLineComponents_ComponentType",
                schema: "HumanResources",
                table: "PayrollRunLineComponents",
                column: "ComponentType");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunLineComponents_PayrollRuleComponentId",
                schema: "HumanResources",
                table: "PayrollRunLineComponents",
                column: "PayrollRuleComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunLineComponents_PayrollRunId",
                schema: "HumanResources",
                table: "PayrollRunLineComponents",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunLineComponents_PayrollRunLineId",
                schema: "HumanResources",
                table: "PayrollRunLineComponents",
                column: "PayrollRunLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunLineComponents_SortOrder",
                schema: "HumanResources",
                table: "PayrollRunLineComponents",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunLines_BusinessId",
                schema: "HumanResources",
                table: "PayrollRunLines",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunLines_EmployeeId",
                schema: "HumanResources",
                table: "PayrollRunLines",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunLines_EmploymentContractId",
                schema: "HumanResources",
                table: "PayrollRunLines",
                column: "EmploymentContractId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunLines_PayrollRunId",
                schema: "HumanResources",
                table: "PayrollRunLines",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunLines_PayrollRunId_EmployeeId",
                schema: "HumanResources",
                table: "PayrollRunLines",
                columns: new[] { "PayrollRunId", "EmployeeId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_BusinessId",
                schema: "HumanResources",
                table: "PayrollRuns",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_BusinessId_PayrollPeriodId_PayrollRuleSetId",
                schema: "HumanResources",
                table: "PayrollRuns",
                columns: new[] { "BusinessId", "PayrollPeriodId", "PayrollRuleSetId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_BusinessId_RunNumber",
                schema: "HumanResources",
                table: "PayrollRuns",
                columns: new[] { "BusinessId", "RunNumber" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_PayrollPeriodId",
                schema: "HumanResources",
                table: "PayrollRuns",
                column: "PayrollPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_PayrollRuleSetId",
                schema: "HumanResources",
                table: "PayrollRuns",
                column: "PayrollRuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_PeriodEndUtc",
                schema: "HumanResources",
                table: "PayrollRuns",
                column: "PeriodEndUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_PeriodStartUtc",
                schema: "HumanResources",
                table: "PayrollRuns",
                column: "PeriodStartUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_Status",
                schema: "HumanResources",
                table: "PayrollRuns",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollRunLineComponents",
                schema: "HumanResources");

            migrationBuilder.DropTable(
                name: "PayrollRunLines",
                schema: "HumanResources");

            migrationBuilder.DropTable(
                name: "PayrollRuns",
                schema: "HumanResources");
        }
    }
}
