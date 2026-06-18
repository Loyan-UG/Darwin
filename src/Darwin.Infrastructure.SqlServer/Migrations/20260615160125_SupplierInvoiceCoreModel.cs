using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class SupplierInvoiceCoreModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierInvoices",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupplierInvoiceNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    InternalInvoiceNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InvoiceDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    TotalNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalTaxMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    MatchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_SupplierInvoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierInvoiceLines",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupplierSku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    InvoicedQuantity = table.Column<int>(type: "int", nullable: false),
                    UnitNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    UnitTaxMinor = table.Column<long>(type: "bigint", nullable: false),
                    UnitGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalTaxMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    MatchStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DiscrepancyReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_SupplierInvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierInvoiceLines_SupplierInvoices_SupplierInvoiceId",
                        column: x => x.SupplierInvoiceId,
                        principalSchema: "Billing",
                        principalTable: "SupplierInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceLines_GoodsReceiptLineId",
                schema: "Billing",
                table: "SupplierInvoiceLines",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceLines_MatchStatus",
                schema: "Billing",
                table: "SupplierInvoiceLines",
                column: "MatchStatus");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceLines_ProductVariantId",
                schema: "Billing",
                table: "SupplierInvoiceLines",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceLines_PurchaseOrderLineId",
                schema: "Billing",
                table: "SupplierInvoiceLines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceLines_SupplierInvoiceId",
                schema: "Billing",
                table: "SupplierInvoiceLines",
                column: "SupplierInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_BusinessId",
                schema: "Billing",
                table: "SupplierInvoices",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_DueDateUtc",
                schema: "Billing",
                table: "SupplierInvoices",
                column: "DueDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_GoodsReceiptId",
                schema: "Billing",
                table: "SupplierInvoices",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_InvoiceDateUtc",
                schema: "Billing",
                table: "SupplierInvoices",
                column: "InvoiceDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_PurchaseOrderId",
                schema: "Billing",
                table: "SupplierInvoices",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_Status",
                schema: "Billing",
                table: "SupplierInvoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_SupplierId",
                schema: "Billing",
                table: "SupplierInvoices",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "UX_SupplierInvoices_Business_InternalNumber_Active",
                schema: "Billing",
                table: "SupplierInvoices",
                columns: new[] { "BusinessId", "InternalInvoiceNumber" },
                unique: true,
                filter: "[InternalInvoiceNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_SupplierInvoices_Business_Supplier_Number_Active",
                schema: "Billing",
                table: "SupplierInvoices",
                columns: new[] { "BusinessId", "SupplierId", "SupplierInvoiceNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierInvoiceLines",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "SupplierInvoices",
                schema: "Billing");
        }
    }
}
