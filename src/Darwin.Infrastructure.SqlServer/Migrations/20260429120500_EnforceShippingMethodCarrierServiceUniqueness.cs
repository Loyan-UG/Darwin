using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DarwinDbContext))]
    [Migration("20260429120500_EnforceShippingMethodCarrierServiceUniqueness")]
    public partial class EnforceShippingMethodCarrierServiceUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_ShippingMethods_ActiveCarrierService",
                schema: "Shipping",
                table: "ShippingMethods",
                columns: new[] { "Carrier", "Service" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ShippingMethods_ActiveCarrierService",
                schema: "Shipping",
                table: "ShippingMethods");
        }
    }
}
