using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ShipmentCarrierEventTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShipmentCarrierEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Carrier = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProviderShipmentReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CarrierEventKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderStatus = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TrackingNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LabelUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Service = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentCarrierEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentCarrierEvents_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentCarrierEvents_ProviderShipmentReference_Carrier_OccurredAtUtc",
                table: "ShipmentCarrierEvents",
                columns: new[] { "ProviderShipmentReference", "Carrier", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentCarrierEvents_ShipmentId_OccurredAtUtc",
                table: "ShipmentCarrierEvents",
                columns: new[] { "ShipmentId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShipmentCarrierEvents");
        }
    }
}
