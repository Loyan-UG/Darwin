using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryNoteCoreModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryNotes",
                schema: "Sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryNoteNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PreparedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IssuedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShippedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveredByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreparedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IssuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShippedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Carrier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Service = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TrackingNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ProviderShipmentReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ShippingAddressJson = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    TotalNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalTaxMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalQuantity = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_DeliveryNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryNotes_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "Businesses",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryNotes_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "CRM",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryNotes_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Orders",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryNotes_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "Orders",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryNotes_Users_CancelledByUserId",
                        column: x => x.CancelledByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryNotes_Users_DeliveredByUserId",
                        column: x => x.DeliveredByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryNotes_Users_IssuedByUserId",
                        column: x => x.IssuedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryNotes_Users_PreparedByUserId",
                        column: x => x.PreparedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryNotes_Users_ShippedByUserId",
                        column: x => x.ShippedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryNoteLines",
                schema: "Sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPriceNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    UnitPriceGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalTaxMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalGrossMinor = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_DeliveryNoteLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryNoteLines_DeliveryNotes_DeliveryNoteId",
                        column: x => x.DeliveryNoteId,
                        principalSchema: "Sales",
                        principalTable: "DeliveryNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNoteLines_DeliveryNoteId",
                schema: "Sales",
                table: "DeliveryNoteLines",
                column: "DeliveryNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNoteLines_Note_SortOrder",
                schema: "Sales",
                table: "DeliveryNoteLines",
                columns: new[] { "DeliveryNoteId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNoteLines_OrderLineId",
                schema: "Sales",
                table: "DeliveryNoteLines",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNoteLines_ProductVariantId",
                schema: "Sales",
                table: "DeliveryNoteLines",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_BusinessId",
                schema: "Sales",
                table: "DeliveryNotes",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_CancelledByUserId",
                schema: "Sales",
                table: "DeliveryNotes",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_CreatedAtUtc",
                schema: "Sales",
                table: "DeliveryNotes",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_CustomerId",
                schema: "Sales",
                table: "DeliveryNotes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_DeliveredByUserId",
                schema: "Sales",
                table: "DeliveryNotes",
                column: "DeliveredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_DeliveryNoteNumber",
                schema: "Sales",
                table: "DeliveryNotes",
                column: "DeliveryNoteNumber",
                unique: true,
                filter: "\"DeliveryNoteNumber\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_IssuedAtUtc",
                schema: "Sales",
                table: "DeliveryNotes",
                column: "IssuedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_IssuedByUserId",
                schema: "Sales",
                table: "DeliveryNotes",
                column: "IssuedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_OrderId",
                schema: "Sales",
                table: "DeliveryNotes",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_PreparedByUserId",
                schema: "Sales",
                table: "DeliveryNotes",
                column: "PreparedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_ShipmentId",
                schema: "Sales",
                table: "DeliveryNotes",
                column: "ShipmentId",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_ShippedByUserId",
                schema: "Sales",
                table: "DeliveryNotes",
                column: "ShippedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_Status",
                schema: "Sales",
                table: "DeliveryNotes",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryNoteLines",
                schema: "Sales");

            migrationBuilder.DropTable(
                name: "DeliveryNotes",
                schema: "Sales");
        }
    }
}
