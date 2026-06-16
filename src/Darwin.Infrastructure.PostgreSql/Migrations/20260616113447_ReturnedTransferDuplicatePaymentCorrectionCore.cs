using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class ReturnedTransferDuplicatePaymentCorrectionCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierPaymentBankCorrections",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankReconciliationMatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankStatementLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginalBankSettlementJournalEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrectionJournalEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrectionType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CorrectionDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    InternalNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierPaymentBankCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierPaymentBankCorrections_BankReconciliationMatches_Ba~",
                        column: x => x.BankReconciliationMatchId,
                        principalSchema: "Billing",
                        principalTable: "BankReconciliationMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierPaymentBankCorrections_BankStatementLines_BankState~",
                        column: x => x.BankStatementLineId,
                        principalSchema: "Billing",
                        principalTable: "BankStatementLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierPaymentBankCorrections_SupplierPayments_SupplierPay~",
                        column: x => x.SupplierPaymentId,
                        principalSchema: "Billing",
                        principalTable: "SupplierPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPaymentBankCorrections_BankReconciliationMatchId",
                schema: "Billing",
                table: "SupplierPaymentBankCorrections",
                column: "BankReconciliationMatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPaymentBankCorrections_BankStatementLineId",
                schema: "Billing",
                table: "SupplierPaymentBankCorrections",
                column: "BankStatementLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPaymentBankCorrections_BusinessId",
                schema: "Billing",
                table: "SupplierPaymentBankCorrections",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPaymentBankCorrections_CorrectionDateUtc",
                schema: "Billing",
                table: "SupplierPaymentBankCorrections",
                column: "CorrectionDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPaymentBankCorrections_CorrectionJournalEntryId",
                schema: "Billing",
                table: "SupplierPaymentBankCorrections",
                column: "CorrectionJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPaymentBankCorrections_CorrectionType",
                schema: "Billing",
                table: "SupplierPaymentBankCorrections",
                column: "CorrectionType");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPaymentBankCorrections_OriginalBankSettlementJourna~",
                schema: "Billing",
                table: "SupplierPaymentBankCorrections",
                column: "OriginalBankSettlementJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPaymentBankCorrections_PostedAtUtc",
                schema: "Billing",
                table: "SupplierPaymentBankCorrections",
                column: "PostedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPaymentBankCorrections_Status",
                schema: "Billing",
                table: "SupplierPaymentBankCorrections",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPaymentBankCorrections_SupplierPaymentId",
                schema: "Billing",
                table: "SupplierPaymentBankCorrections",
                column: "SupplierPaymentId");

            migrationBuilder.CreateIndex(
                name: "UX_SupplierPaymentBankCorrections_Payment_Type_Reconciliation_Active",
                schema: "Billing",
                table: "SupplierPaymentBankCorrections",
                columns: new[] { "SupplierPaymentId", "CorrectionType", "BankReconciliationMatchId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierPaymentBankCorrections",
                schema: "Billing");
        }
    }
}
