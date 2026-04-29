using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DarwinDbContext))]
    [Migration("20260429113000_EnforcePendingShipmentProviderOperationIdempotency")]
    public partial class EnforcePendingShipmentProviderOperationIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_ShipmentProviderOperations_ActivePending",
                schema: "Integration",
                table: "ShipmentProviderOperations",
                columns: new[] { "ShipmentId", "Provider", "OperationType" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [Status] = N'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ShipmentProviderOperations_ActivePending",
                schema: "Integration",
                table: "ShipmentProviderOperations");
        }
    }
}
