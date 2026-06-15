using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class FinanceExportBatchFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceExportBatches",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExportKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PostingStatusMode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PackageHashSha256 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PackageContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PackageFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    ErrorSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_FinanceExportBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceExportBatches_ExternalSystems_ExternalSystemId",
                        column: x => x.ExternalSystemId,
                        principalSchema: "Integration",
                        principalTable: "ExternalSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceExportAttempts",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FinanceExportBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PackageHashSha256 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ErrorSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_FinanceExportAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceExportAttempts_FinanceExportBatches_FinanceExportBatchId",
                        column: x => x.FinanceExportBatchId,
                        principalSchema: "Billing",
                        principalTable: "FinanceExportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExportAttempts_FinanceExportBatchId",
                schema: "Billing",
                table: "FinanceExportAttempts",
                column: "FinanceExportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExportAttempts_Status",
                schema: "Billing",
                table: "FinanceExportAttempts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_FinanceExportAttempts_Batch_AttemptNumber_Active",
                schema: "Billing",
                table: "FinanceExportAttempts",
                columns: new[] { "FinanceExportBatchId", "AttemptNumber" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExportBatches_BusinessId",
                schema: "Billing",
                table: "FinanceExportBatches",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExportBatches_ExternalSystemId",
                schema: "Billing",
                table: "FinanceExportBatches",
                column: "ExternalSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExportBatches_Status",
                schema: "Billing",
                table: "FinanceExportBatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceExportBatches_BusinessId_PeriodStartUtc_PeriodEndUtc",
                schema: "Billing",
                table: "FinanceExportBatches",
                columns: new[] { "BusinessId", "PeriodStartUtc", "PeriodEndUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_FinanceExportBatches_Business_Target_Key_Active",
                schema: "Billing",
                table: "FinanceExportBatches",
                columns: new[] { "BusinessId", "ExternalSystemId", "ExportKey" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinanceExportAttempts",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "FinanceExportBatches",
                schema: "Billing");
        }
    }
}
