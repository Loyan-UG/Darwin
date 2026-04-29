using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class EnforceProviderCallbackInboxIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProviderCallbackInboxMessages_IdempotencyKey",
                schema: "Integration",
                table: "ProviderCallbackInboxMessages");

            migrationBuilder.CreateIndex(
                name: "UX_ProviderCallbackInboxMessages_Provider_IdempotencyKey",
                schema: "Integration",
                table: "ProviderCallbackInboxMessages",
                columns: new[] { "Provider", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL AND \"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ProviderCallbackInboxMessages_Provider_IdempotencyKey",
                schema: "Integration",
                table: "ProviderCallbackInboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCallbackInboxMessages_IdempotencyKey",
                schema: "Integration",
                table: "ProviderCallbackInboxMessages",
                column: "IdempotencyKey");
        }
    }
}
