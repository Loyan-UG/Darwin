using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class HrLeaveAbsenceCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LeaveType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StartDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestedMinutes = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PrivacyClassification = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
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
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "HumanResources",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AbsenceRecords",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaveRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    AbsenceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StartDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AbsenceMinutes = table.Column<int>(type: "integer", nullable: false),
                    PrivacyClassification = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
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
                    table.PrimaryKey("PK_AbsenceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbsenceRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "HumanResources",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AbsenceRecords_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalSchema: "HumanResources",
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRecords_AbsenceType",
                schema: "HumanResources",
                table: "AbsenceRecords",
                column: "AbsenceType");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRecords_BusinessId",
                schema: "HumanResources",
                table: "AbsenceRecords",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRecords_EmployeeId",
                schema: "HumanResources",
                table: "AbsenceRecords",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRecords_EndDateUtc",
                schema: "HumanResources",
                table: "AbsenceRecords",
                column: "EndDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRecords_LeaveRequestId",
                schema: "HumanResources",
                table: "AbsenceRecords",
                column: "LeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRecords_StartDateUtc",
                schema: "HumanResources",
                table: "AbsenceRecords",
                column: "StartDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRecords_Status",
                schema: "HumanResources",
                table: "AbsenceRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_BusinessId",
                schema: "HumanResources",
                table: "LeaveRequests",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_BusinessId_EmployeeId_RequestNumber",
                schema: "HumanResources",
                table: "LeaveRequests",
                columns: new[] { "BusinessId", "EmployeeId", "RequestNumber" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmployeeId",
                schema: "HumanResources",
                table: "LeaveRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EndDateUtc",
                schema: "HumanResources",
                table: "LeaveRequests",
                column: "EndDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LeaveType",
                schema: "HumanResources",
                table: "LeaveRequests",
                column: "LeaveType");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_StartDateUtc",
                schema: "HumanResources",
                table: "LeaveRequests",
                column: "StartDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_Status",
                schema: "HumanResources",
                table: "LeaveRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbsenceRecords",
                schema: "HumanResources");

            migrationBuilder.DropTable(
                name: "LeaveRequests",
                schema: "HumanResources");
        }
    }
}
