using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignSqlServerSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "ShippingRates",
                newName: "ShippingRates",
                newSchema: "Shipping");

            migrationBuilder.RenameTable(
                name: "EmailDispatchAudits",
                newName: "EmailDispatchAudits",
                newSchema: "Integration");

            migrationBuilder.RenameTable(
                name: "ChannelDispatchAudits",
                newName: "ChannelDispatchAudits",
                newSchema: "Integration");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "ShippingRates",
                schema: "Shipping",
                newName: "ShippingRates");

            migrationBuilder.RenameTable(
                name: "EmailDispatchAudits",
                schema: "Integration",
                newName: "EmailDispatchAudits");

            migrationBuilder.RenameTable(
                name: "ChannelDispatchAudits",
                schema: "Integration",
                newName: "ChannelDispatchAudits");
        }
    }
}
