using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionCheckoutSessionReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderCheckoutSessionId",
                schema: "Billing",
                table: "BusinessSubscriptions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSubscriptions_Provider_ProviderCheckoutSessionId",
                schema: "Billing",
                table: "BusinessSubscriptions",
                columns: new[] { "Provider", "ProviderCheckoutSessionId" },
                unique: true,
                filter: "\"ProviderCheckoutSessionId\" IS NOT NULL AND \"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BusinessSubscriptions_Provider_ProviderCheckoutSessionId",
                schema: "Billing",
                table: "BusinessSubscriptions");

            migrationBuilder.DropColumn(
                name: "ProviderCheckoutSessionId",
                schema: "Billing",
                table: "BusinessSubscriptions");
        }
    }
}
