using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class HrTimeAttendanceCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceEvents",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_AttendanceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceEvents_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "HumanResources",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Timesheets",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimesheetNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalWorkMinutes = table.Column<int>(type: "int", nullable: false),
                    TotalBreakMinutes = table.Column<int>(type: "int", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_Timesheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timesheets_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "HumanResources",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkSchedules",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MondayMinutes = table.Column<int>(type: "int", nullable: false),
                    TuesdayMinutes = table.Column<int>(type: "int", nullable: false),
                    WednesdayMinutes = table.Column<int>(type: "int", nullable: false),
                    ThursdayMinutes = table.Column<int>(type: "int", nullable: false),
                    FridayMinutes = table.Column<int>(type: "int", nullable: false),
                    SaturdayMinutes = table.Column<int>(type: "int", nullable: false),
                    SundayMinutes = table.Column<int>(type: "int", nullable: false),
                    InternalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_WorkSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkSchedules_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "HumanResources",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TimeEntries",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    BreakMinutes = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_TimeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeEntries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "HumanResources",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeEntries_WorkSchedules_WorkScheduleId",
                        column: x => x.WorkScheduleId,
                        principalSchema: "HumanResources",
                        principalTable: "WorkSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkScheduleExceptions",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledMinutes = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InternalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_WorkScheduleExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkScheduleExceptions_WorkSchedules_WorkScheduleId",
                        column: x => x.WorkScheduleId,
                        principalSchema: "HumanResources",
                        principalTable: "WorkSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimesheetLines",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimesheetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    BreakMinutes = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimesheetLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimesheetLines_TimeEntries_TimeEntryId",
                        column: x => x.TimeEntryId,
                        principalSchema: "HumanResources",
                        principalTable: "TimeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimesheetLines_Timesheets_TimesheetId",
                        column: x => x.TimesheetId,
                        principalSchema: "HumanResources",
                        principalTable: "Timesheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvents_BusinessId",
                schema: "HumanResources",
                table: "AttendanceEvents",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvents_EmployeeId",
                schema: "HumanResources",
                table: "AttendanceEvents",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvents_EventType",
                schema: "HumanResources",
                table: "AttendanceEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvents_OccurredAtUtc",
                schema: "HumanResources",
                table: "AttendanceEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_BusinessId",
                schema: "HumanResources",
                table: "TimeEntries",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_EmployeeId",
                schema: "HumanResources",
                table: "TimeEntries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_Status",
                schema: "HumanResources",
                table: "TimeEntries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_WorkDateUtc",
                schema: "HumanResources",
                table: "TimeEntries",
                column: "WorkDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_WorkScheduleId",
                schema: "HumanResources",
                table: "TimeEntries",
                column: "WorkScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetLines_BusinessId",
                schema: "HumanResources",
                table: "TimesheetLines",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetLines_TimeEntryId",
                schema: "HumanResources",
                table: "TimesheetLines",
                column: "TimeEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetLines_TimesheetId",
                schema: "HumanResources",
                table: "TimesheetLines",
                column: "TimesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetLines_TimesheetId_TimeEntryId",
                schema: "HumanResources",
                table: "TimesheetLines",
                columns: new[] { "TimesheetId", "TimeEntryId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_BusinessId",
                schema: "HumanResources",
                table: "Timesheets",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_BusinessId_EmployeeId_PeriodStartUtc_PeriodEndUtc",
                schema: "HumanResources",
                table: "Timesheets",
                columns: new[] { "BusinessId", "EmployeeId", "PeriodStartUtc", "PeriodEndUtc" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_BusinessId_EmployeeId_TimesheetNumber",
                schema: "HumanResources",
                table: "Timesheets",
                columns: new[] { "BusinessId", "EmployeeId", "TimesheetNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_EmployeeId",
                schema: "HumanResources",
                table: "Timesheets",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_PeriodEndUtc",
                schema: "HumanResources",
                table: "Timesheets",
                column: "PeriodEndUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_PeriodStartUtc",
                schema: "HumanResources",
                table: "Timesheets",
                column: "PeriodStartUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_Status",
                schema: "HumanResources",
                table: "Timesheets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleExceptions_BusinessId",
                schema: "HumanResources",
                table: "WorkScheduleExceptions",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleExceptions_WorkDateUtc",
                schema: "HumanResources",
                table: "WorkScheduleExceptions",
                column: "WorkDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleExceptions_WorkScheduleId",
                schema: "HumanResources",
                table: "WorkScheduleExceptions",
                column: "WorkScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleExceptions_WorkScheduleId_WorkDateUtc",
                schema: "HumanResources",
                table: "WorkScheduleExceptions",
                columns: new[] { "WorkScheduleId", "WorkDateUtc" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_BusinessId",
                schema: "HumanResources",
                table: "WorkSchedules",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_BusinessId_EmployeeId_ScheduleCode",
                schema: "HumanResources",
                table: "WorkSchedules",
                columns: new[] { "BusinessId", "EmployeeId", "ScheduleCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_EffectiveFromUtc",
                schema: "HumanResources",
                table: "WorkSchedules",
                column: "EffectiveFromUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_EffectiveToUtc",
                schema: "HumanResources",
                table: "WorkSchedules",
                column: "EffectiveToUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_EmployeeId",
                schema: "HumanResources",
                table: "WorkSchedules",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_Status",
                schema: "HumanResources",
                table: "WorkSchedules",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceEvents",
                schema: "HumanResources");

            migrationBuilder.DropTable(
                name: "TimesheetLines",
                schema: "HumanResources");

            migrationBuilder.DropTable(
                name: "WorkScheduleExceptions",
                schema: "HumanResources");

            migrationBuilder.DropTable(
                name: "TimeEntries",
                schema: "HumanResources");

            migrationBuilder.DropTable(
                name: "Timesheets",
                schema: "HumanResources");

            migrationBuilder.DropTable(
                name: "WorkSchedules",
                schema: "HumanResources");
        }
    }
}
