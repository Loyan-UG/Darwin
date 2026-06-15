using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class SalesQuoteCoreModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Sales");

            migrationBuilder.CreateTable(
                name: "SalesQuotes",
                schema: "Sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OpportunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConvertedOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuoteNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ValidUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreparedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SentByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcceptedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConvertedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConvertedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalTaxMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    CustomerSnapshotJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    BillingAddressJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    ShippingAddressJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    InternalNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "Businesses",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "CRM",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalSchema: "CRM",
                        principalTable: "Opportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Orders_ConvertedOrderId",
                        column: x => x.ConvertedOrderId,
                        principalSchema: "Orders",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Users_AcceptedByUserId",
                        column: x => x.AcceptedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Users_ConvertedByUserId",
                        column: x => x.ConvertedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Users_PreparedByUserId",
                        column: x => x.PreparedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Users_RejectedByUserId",
                        column: x => x.RejectedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Users_SentByUserId",
                        column: x => x.SentByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesQuoteLines",
                schema: "Sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesQuoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPriceNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    UnitPriceGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalTaxMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalGrossMinor = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_SalesQuoteLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesQuoteLines_SalesQuotes_SalesQuoteId",
                        column: x => x.SalesQuoteId,
                        principalSchema: "Sales",
                        principalTable: "SalesQuotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuoteLines_ProductVariantId",
                schema: "Sales",
                table: "SalesQuoteLines",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuoteLines_Quote_SortOrder",
                schema: "Sales",
                table: "SalesQuoteLines",
                columns: new[] { "SalesQuoteId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuoteLines_SalesQuoteId",
                schema: "Sales",
                table: "SalesQuoteLines",
                column: "SalesQuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_AcceptedByUserId",
                schema: "Sales",
                table: "SalesQuotes",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_BusinessId",
                schema: "Sales",
                table: "SalesQuotes",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_ConvertedByUserId",
                schema: "Sales",
                table: "SalesQuotes",
                column: "ConvertedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_ConvertedOrderId",
                schema: "Sales",
                table: "SalesQuotes",
                column: "ConvertedOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_CreatedAtUtc",
                schema: "Sales",
                table: "SalesQuotes",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_CustomerId",
                schema: "Sales",
                table: "SalesQuotes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_OpportunityId",
                schema: "Sales",
                table: "SalesQuotes",
                column: "OpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_OwnerUserId",
                schema: "Sales",
                table: "SalesQuotes",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_PreparedByUserId",
                schema: "Sales",
                table: "SalesQuotes",
                column: "PreparedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_QuoteNumber",
                schema: "Sales",
                table: "SalesQuotes",
                column: "QuoteNumber",
                unique: true,
                filter: "[QuoteNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_RejectedByUserId",
                schema: "Sales",
                table: "SalesQuotes",
                column: "RejectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_SentAtUtc",
                schema: "Sales",
                table: "SalesQuotes",
                column: "SentAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_SentByUserId",
                schema: "Sales",
                table: "SalesQuotes",
                column: "SentByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_Status",
                schema: "Sales",
                table: "SalesQuotes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_ValidUntilUtc",
                schema: "Sales",
                table: "SalesQuotes",
                column: "ValidUntilUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesQuoteLines",
                schema: "Sales");

            migrationBuilder.DropTable(
                name: "SalesQuotes",
                schema: "Sales");
        }
    }
}
