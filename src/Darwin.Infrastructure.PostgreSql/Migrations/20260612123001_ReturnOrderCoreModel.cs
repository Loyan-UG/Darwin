using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class ReturnOrderCoreModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReturnOrders",
                schema: "Sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReturnOrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CustomerSnapshotJson = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    ShippingAddressJson = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    InternalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 16000, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReturnShipmentQueuedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    InspectedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefundReadyByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefundedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReturnShipmentQueuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InspectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundReadyAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequestedQuantity = table.Column<int>(type: "integer", nullable: false),
                    ApprovedQuantity = table.Column<int>(type: "integer", nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "integer", nullable: false),
                    AcceptedQuantity = table.Column<int>(type: "integer", nullable: false),
                    RejectedQuantity = table.Column<int>(type: "integer", nullable: false),
                    ScrappedQuantity = table.Column<int>(type: "integer", nullable: false),
                    RestockQuantity = table.Column<int>(type: "integer", nullable: false),
                    RequestedGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    ApprovedGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    AcceptedGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    RefundEligibleGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnOrders", x => x.Id);
                    table.ForeignKey("FK_ReturnOrders_Businesses_BusinessId", x => x.BusinessId, principalSchema: "Businesses", principalTable: "Businesses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ReturnOrders_Customers_CustomerId", x => x.CustomerId, principalSchema: "CRM", principalTable: "Customers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ReturnOrders_Invoices_InvoiceId", x => x.InvoiceId, principalSchema: "CRM", principalTable: "Invoices", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ReturnOrders_Orders_OrderId", x => x.OrderId, principalSchema: "Orders", principalTable: "Orders", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ReturnOrders_Shipments_ShipmentId", x => x.ShipmentId, principalSchema: "Orders", principalTable: "Shipments", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReturnOrderLines",
                schema: "Sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    RestockWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequestedQuantity = table.Column<int>(type: "integer", nullable: false),
                    ApprovedQuantity = table.Column<int>(type: "integer", nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "integer", nullable: false),
                    AcceptedQuantity = table.Column<int>(type: "integer", nullable: false),
                    RejectedQuantity = table.Column<int>(type: "integer", nullable: false),
                    ScrappedQuantity = table.Column<int>(type: "integer", nullable: false),
                    RestockQuantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPriceNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    UnitPriceGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    RequestedNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    RequestedTaxMinor = table.Column<long>(type: "bigint", nullable: false),
                    RequestedGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    ApprovedNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    ApprovedTaxMinor = table.Column<long>(type: "bigint", nullable: false),
                    ApprovedGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    AcceptedNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    AcceptedTaxMinor = table.Column<long>(type: "bigint", nullable: false),
                    AcceptedGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    Disposition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
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
                    table.PrimaryKey("PK_ReturnOrderLines", x => x.Id);
                    table.ForeignKey("FK_ReturnOrderLines_ReturnOrders_ReturnOrderId", x => x.ReturnOrderId, principalSchema: "Sales", principalTable: "ReturnOrders", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReturnOrderRefundLinks",
                schema: "Sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefundId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnOrderRefundLinks", x => x.Id);
                    table.ForeignKey("FK_ReturnOrderRefundLinks_Refunds_RefundId", x => x.RefundId, principalSchema: "Orders", principalTable: "Refunds", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ReturnOrderRefundLinks_ReturnOrders_ReturnOrderId", x => x.ReturnOrderId, principalSchema: "Sales", principalTable: "ReturnOrders", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_ReturnOrders_ReturnOrderNumber", "ReturnOrders", "ReturnOrderNumber", schema: "Sales", unique: true, filter: "\"ReturnOrderNumber\" IS NOT NULL AND \"IsDeleted\" = FALSE");
            migrationBuilder.CreateIndex("IX_ReturnOrders_OrderId", "ReturnOrders", "OrderId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrders_ShipmentId", "ReturnOrders", "ShipmentId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrders_InvoiceId", "ReturnOrders", "InvoiceId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrders_Status", "ReturnOrders", "Status", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrders_BusinessId", "ReturnOrders", "BusinessId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrders_CustomerId", "ReturnOrders", "CustomerId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrders_ApprovedAtUtc", "ReturnOrders", "ApprovedAtUtc", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrders_ReceivedAtUtc", "ReturnOrders", "ReceivedAtUtc", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrders_InspectedAtUtc", "ReturnOrders", "InspectedAtUtc", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrders_RefundedAtUtc", "ReturnOrders", "RefundedAtUtc", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrders_CreatedAtUtc", "ReturnOrders", "CreatedAtUtc", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrderLines_ReturnOrderId", "ReturnOrderLines", "ReturnOrderId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrderLines_OrderLineId", "ReturnOrderLines", "OrderLineId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrderLines_ShipmentLineId", "ReturnOrderLines", "ShipmentLineId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrderLines_ProductVariantId", "ReturnOrderLines", "ProductVariantId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrderLines_RestockWarehouseId", "ReturnOrderLines", "RestockWarehouseId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrderLines_ReturnOrder_SortOrder", "ReturnOrderLines", new[] { "ReturnOrderId", "SortOrder" }, schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrderRefundLinks_ReturnOrderId", "ReturnOrderRefundLinks", "ReturnOrderId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrderRefundLinks_RefundId", "ReturnOrderRefundLinks", "RefundId", schema: "Sales");
            migrationBuilder.CreateIndex("IX_ReturnOrderRefundLinks_ReturnOrder_Refund", "ReturnOrderRefundLinks", new[] { "ReturnOrderId", "RefundId" }, schema: "Sales", unique: true, filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ReturnOrderRefundLinks", schema: "Sales");
            migrationBuilder.DropTable(name: "ReturnOrderLines", schema: "Sales");
            migrationBuilder.DropTable(name: "ReturnOrders", schema: "Sales");
        }
    }
}
