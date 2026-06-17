using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class PayrollPaymentCoreModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollPayments",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PaymentDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TotalAmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PostingJournalEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    PostedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InternalNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_PayrollPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollPayments_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalSchema: "HumanResources",
                        principalTable: "PayrollRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollPaymentAllocations",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRunLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Memo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollPaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollPaymentAllocations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "HumanResources",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPaymentAllocations_PayrollPayments_PayrollPaymentId",
                        column: x => x.PayrollPaymentId,
                        principalSchema: "HumanResources",
                        principalTable: "PayrollPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayrollPaymentAllocations_PayrollRunLines_PayrollRunLineId",
                        column: x => x.PayrollRunLineId,
                        principalSchema: "HumanResources",
                        principalTable: "PayrollRunLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPaymentAllocations_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalSchema: "HumanResources",
                        principalTable: "PayrollRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentAllocations_BusinessId",
                schema: "HumanResources",
                table: "PayrollPaymentAllocations",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentAllocations_EmployeeId",
                schema: "HumanResources",
                table: "PayrollPaymentAllocations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentAllocations_PayrollPaymentId",
                schema: "HumanResources",
                table: "PayrollPaymentAllocations",
                column: "PayrollPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentAllocations_PayrollPaymentId_PayrollRunLineId",
                schema: "HumanResources",
                table: "PayrollPaymentAllocations",
                columns: new[] { "PayrollPaymentId", "PayrollRunLineId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentAllocations_PayrollRunId",
                schema: "HumanResources",
                table: "PayrollPaymentAllocations",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentAllocations_PayrollRunLineId",
                schema: "HumanResources",
                table: "PayrollPaymentAllocations",
                column: "PayrollRunLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_BusinessId",
                schema: "HumanResources",
                table: "PayrollPayments",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_BusinessId_PaymentNumber",
                schema: "HumanResources",
                table: "PayrollPayments",
                columns: new[] { "BusinessId", "PaymentNumber" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE AND \"PaymentNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_PaymentDateUtc",
                schema: "HumanResources",
                table: "PayrollPayments",
                column: "PaymentDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_PayrollRunId",
                schema: "HumanResources",
                table: "PayrollPayments",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_PostedAtUtc",
                schema: "HumanResources",
                table: "PayrollPayments",
                column: "PostedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_PostingJournalEntryId",
                schema: "HumanResources",
                table: "PayrollPayments",
                column: "PostingJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_Status",
                schema: "HumanResources",
                table: "PayrollPayments",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollPaymentAllocations",
                schema: "HumanResources");

            migrationBuilder.DropTable(
                name: "PayrollPayments",
                schema: "HumanResources");
        }
    }
}
