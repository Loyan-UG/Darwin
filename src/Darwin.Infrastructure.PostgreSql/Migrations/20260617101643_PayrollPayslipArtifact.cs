using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class PayrollPayslipArtifact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollPayslips",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRunLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayslipNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrossPayMinor = table.Column<long>(type: "bigint", nullable: false),
                    EmployeeDeductionMinor = table.Column<long>(type: "bigint", nullable: false),
                    EmployerCostMinor = table.Column<long>(type: "bigint", nullable: false),
                    NetPayMinor = table.Column<long>(type: "bigint", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SnapshotJson = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: true),
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
                    table.PrimaryKey("PK_PayrollPayslips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollPayslips_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "HumanResources",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPayslips_PayrollRunLines_PayrollRunLineId",
                        column: x => x.PayrollRunLineId,
                        principalSchema: "HumanResources",
                        principalTable: "PayrollRunLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPayslips_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalSchema: "HumanResources",
                        principalTable: "PayrollRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayslips_BusinessId",
                schema: "HumanResources",
                table: "PayrollPayslips",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayslips_BusinessId_PayslipNumber",
                schema: "HumanResources",
                table: "PayrollPayslips",
                columns: new[] { "BusinessId", "PayslipNumber" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayslips_DocumentRecordId",
                schema: "HumanResources",
                table: "PayrollPayslips",
                column: "DocumentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayslips_EmployeeId",
                schema: "HumanResources",
                table: "PayrollPayslips",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayslips_GeneratedAtUtc",
                schema: "HumanResources",
                table: "PayrollPayslips",
                column: "GeneratedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayslips_PayrollRunId",
                schema: "HumanResources",
                table: "PayrollPayslips",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayslips_PayrollRunLineId",
                schema: "HumanResources",
                table: "PayrollPayslips",
                column: "PayrollRunLineId",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayslips_Status",
                schema: "HumanResources",
                table: "PayrollPayslips",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollPayslips",
                schema: "HumanResources");
        }
    }
}
