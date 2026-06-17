using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class SupplierAdvanceCoreModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierAdvances",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdvanceNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AdvanceDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    TotalAmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    OpenAmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PostingJournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierAdvances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierAdvanceApplications",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierAdvanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostingJournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Memo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierAdvanceApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierAdvanceApplications_SupplierAdvances_SupplierAdvanceId",
                        column: x => x.SupplierAdvanceId,
                        principalSchema: "Billing",
                        principalTable: "SupplierAdvances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierAdvanceApplications_SupplierInvoices_SupplierInvoiceId",
                        column: x => x.SupplierInvoiceId,
                        principalSchema: "Billing",
                        principalTable: "SupplierInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAdvanceApplications_AppliedAtUtc",
                schema: "Billing",
                table: "SupplierAdvanceApplications",
                column: "AppliedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAdvanceApplications_PostingJournalEntryId",
                schema: "Billing",
                table: "SupplierAdvanceApplications",
                column: "PostingJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAdvanceApplications_SupplierAdvanceId",
                schema: "Billing",
                table: "SupplierAdvanceApplications",
                column: "SupplierAdvanceId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAdvanceApplications_SupplierInvoiceId",
                schema: "Billing",
                table: "SupplierAdvanceApplications",
                column: "SupplierInvoiceId");

            migrationBuilder.CreateIndex(
                name: "UX_SupplierAdvanceApplications_Advance_Invoice_Active",
                schema: "Billing",
                table: "SupplierAdvanceApplications",
                columns: new[] { "SupplierAdvanceId", "SupplierInvoiceId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAdvances_AdvanceDateUtc",
                schema: "Billing",
                table: "SupplierAdvances",
                column: "AdvanceDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAdvances_BusinessId",
                schema: "Billing",
                table: "SupplierAdvances",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAdvances_CancelledAtUtc",
                schema: "Billing",
                table: "SupplierAdvances",
                column: "CancelledAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAdvances_PostedAtUtc",
                schema: "Billing",
                table: "SupplierAdvances",
                column: "PostedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAdvances_PostingJournalEntryId",
                schema: "Billing",
                table: "SupplierAdvances",
                column: "PostingJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAdvances_Status",
                schema: "Billing",
                table: "SupplierAdvances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAdvances_SupplierId",
                schema: "Billing",
                table: "SupplierAdvances",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "UX_SupplierAdvances_Business_Number_Active",
                schema: "Billing",
                table: "SupplierAdvances",
                columns: new[] { "BusinessId", "AdvanceNumber" },
                unique: true,
                filter: "[AdvanceNumber] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierAdvanceApplications",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "SupplierAdvances",
                schema: "Billing");
        }
    }
}
