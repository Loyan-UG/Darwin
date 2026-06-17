using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class PayrollReturnedTransferCorrectionCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollPaymentBankCorrections",
                schema: "HumanResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankReconciliationMatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankStatementLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginalBankSettlementJournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrectionJournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrectionType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CorrectionDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_PayrollPaymentBankCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollPaymentBankCorrections_PayrollPayments_PayrollPaymentId",
                        column: x => x.PayrollPaymentId,
                        principalSchema: "HumanResources",
                        principalTable: "PayrollPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentBankCorrections_BankReconciliationMatchId",
                schema: "HumanResources",
                table: "PayrollPaymentBankCorrections",
                column: "BankReconciliationMatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentBankCorrections_BankStatementLineId",
                schema: "HumanResources",
                table: "PayrollPaymentBankCorrections",
                column: "BankStatementLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentBankCorrections_BusinessId",
                schema: "HumanResources",
                table: "PayrollPaymentBankCorrections",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentBankCorrections_CorrectionDateUtc",
                schema: "HumanResources",
                table: "PayrollPaymentBankCorrections",
                column: "CorrectionDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentBankCorrections_CorrectionJournalEntryId",
                schema: "HumanResources",
                table: "PayrollPaymentBankCorrections",
                column: "CorrectionJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentBankCorrections_OriginalBankSettlementJournalEntryId",
                schema: "HumanResources",
                table: "PayrollPaymentBankCorrections",
                column: "OriginalBankSettlementJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentBankCorrections_PayrollPaymentId",
                schema: "HumanResources",
                table: "PayrollPaymentBankCorrections",
                column: "PayrollPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentBankCorrections_PostedAtUtc",
                schema: "HumanResources",
                table: "PayrollPaymentBankCorrections",
                column: "PostedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentBankCorrections_Status",
                schema: "HumanResources",
                table: "PayrollPaymentBankCorrections",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_PayrollPaymentBankCorrections_Payment_Type_Reconciliation_Active",
                schema: "HumanResources",
                table: "PayrollPaymentBankCorrections",
                columns: new[] { "PayrollPaymentId", "CorrectionType", "BankReconciliationMatchId" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [Status] <> 'Cancelled'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollPaymentBankCorrections",
                schema: "HumanResources");
        }
    }
}
