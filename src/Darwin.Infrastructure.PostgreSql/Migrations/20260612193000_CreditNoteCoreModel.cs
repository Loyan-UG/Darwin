using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class CreditNoteCoreModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditNotes",
                schema: "Sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefundId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreditNoteNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    OriginalInvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IssuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IssuedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    VoidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TotalNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalTaxMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    SourceModelJson = table.Column<string>(type: "jsonb", maxLength: 32000, nullable: false),
                    SourceModelHashSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ArchiveGeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArchiveRetainUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArchiveRetentionPolicyVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ArchivePurgedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArchivePurgeReason = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    PostingJournalEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    InternalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 16000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNotes", x => x.Id);
                    table.ForeignKey("FK_CreditNotes_Businesses_BusinessId", x => x.BusinessId, principalSchema: "Businesses", principalTable: "Businesses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_CreditNotes_Customers_CustomerId", x => x.CustomerId, principalSchema: "CRM", principalTable: "Customers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_CreditNotes_Invoices_InvoiceId", x => x.InvoiceId, principalSchema: "CRM", principalTable: "Invoices", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_CreditNotes_JournalEntries_PostingJournalEntryId", x => x.PostingJournalEntryId, principalSchema: "Billing", principalTable: "JournalEntries", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_CreditNotes_Refunds_RefundId", x => x.RefundId, principalSchema: "Orders", principalTable: "Refunds", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_CreditNotes_ReturnOrders_ReturnOrderId", x => x.ReturnOrderId, principalSchema: "Sales", principalTable: "ReturnOrders", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditNoteLines",
                schema: "Sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    OriginalQuantity = table.Column<int>(type: "integer", nullable: false),
                    CreditedQuantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPriceNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalTaxMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    SourceLineJson = table.Column<string>(type: "jsonb", maxLength: 16000, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNoteLines", x => x.Id);
                    table.ForeignKey("FK_CreditNoteLines_CreditNotes_CreditNoteId", x => x.CreditNoteId, principalSchema: "Sales", principalTable: "CreditNotes", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_CreditNoteLines_InvoiceLines_InvoiceLineId", x => x.InvoiceLineId, principalSchema: "CRM", principalTable: "InvoiceLines", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex("IX_CreditNotes_CreditNoteNumber", "CreditNotes", "CreditNoteNumber", schema: "Sales", unique: true, filter: "\"CreditNoteNumber\" IS NOT NULL AND \"IsDeleted\" = FALSE");
            migrationBuilder.CreateIndex("IX_CreditNotes_InvoiceId", "CreditNotes", "InvoiceId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_CreditNotes_ReturnOrderId", "CreditNotes", "ReturnOrderId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_CreditNotes_RefundId", "CreditNotes", "RefundId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_CreditNotes_Status", "CreditNotes", "Status", schema: "Sales");
            migrationBuilder.CreateIndex("IX_CreditNotes_BusinessId", "CreditNotes", "BusinessId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_CreditNotes_CustomerId", "CreditNotes", "CustomerId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_CreditNotes_IssuedAtUtc", "CreditNotes", "IssuedAtUtc", schema: "Sales");
            migrationBuilder.CreateIndex("IX_CreditNotes_ArchiveRetainUntilUtc", "CreditNotes", "ArchiveRetainUntilUtc", schema: "Sales");
            migrationBuilder.CreateIndex("IX_CreditNotes_ArchivePurgedAtUtc", "CreditNotes", "ArchivePurgedAtUtc", schema: "Sales");
            migrationBuilder.CreateIndex("IX_CreditNoteLines_CreditNoteId", "CreditNoteLines", "CreditNoteId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_CreditNoteLines_InvoiceLineId", "CreditNoteLines", "InvoiceLineId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_CreditNoteLines_CreditNote_SortOrder", "CreditNoteLines", new[] { "CreditNoteId", "SortOrder" }, schema: "Sales");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CreditNoteLines", schema: "Sales");
            migrationBuilder.DropTable(name: "CreditNotes", schema: "Sales");
        }
    }
}
