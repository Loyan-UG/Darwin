using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class PayrollPeriodExportSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollPeriods",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmployeeCount = table.Column<int>(type: "integer", nullable: false),
                    TotalWorkMinutes = table.Column<int>(type: "integer", nullable: false),
                    TotalBreakMinutes = table.Column<int>(type: "integer", nullable: false),
                    TotalAbsenceMinutes = table.Column<int>(type: "integer", nullable: false),
                    PreparedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    InternalNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_PayrollPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollPeriodLines",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkMinutes = table.Column<int>(type: "integer", nullable: false),
                    BreakMinutes = table.Column<int>(type: "integer", nullable: false),
                    AbsenceMinutes = table.Column<int>(type: "integer", nullable: false),
                    ApprovedTimesheetCount = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedAbsenceCount = table.Column<int>(type: "integer", nullable: false),
                    SummaryJson = table.Column<string>(type: "jsonb", maxLength: 8000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollPeriodLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollPeriodLines_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "HumanResources",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPeriodLines_PayrollPeriods_PayrollPeriodId",
                        column: x => x.PayrollPeriodId,
                        principalSchema: "HumanResources",
                        principalTable: "PayrollPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriodLines_BusinessId",
                schema: "HumanResources",
                table: "PayrollPeriodLines",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriodLines_EmployeeId",
                schema: "HumanResources",
                table: "PayrollPeriodLines",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriodLines_PayrollPeriodId",
                schema: "HumanResources",
                table: "PayrollPeriodLines",
                column: "PayrollPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriodLines_PayrollPeriodId_EmployeeId",
                schema: "HumanResources",
                table: "PayrollPeriodLines",
                columns: new[] { "PayrollPeriodId", "EmployeeId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_BusinessId",
                schema: "HumanResources",
                table: "PayrollPeriods",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_BusinessId_PeriodCode",
                schema: "HumanResources",
                table: "PayrollPeriods",
                columns: new[] { "BusinessId", "PeriodCode" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_BusinessId_PeriodStartUtc_PeriodEndUtc",
                schema: "HumanResources",
                table: "PayrollPeriods",
                columns: new[] { "BusinessId", "PeriodStartUtc", "PeriodEndUtc" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_PeriodEndUtc",
                schema: "HumanResources",
                table: "PayrollPeriods",
                column: "PeriodEndUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_PeriodStartUtc",
                schema: "HumanResources",
                table: "PayrollPeriods",
                column: "PeriodStartUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_Status",
                schema: "HumanResources",
                table: "PayrollPeriods",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollPeriodLines",
                schema: "HumanResources");

            migrationBuilder.DropTable(
                name: "PayrollPeriods",
                schema: "HumanResources");
        }
    }
}
