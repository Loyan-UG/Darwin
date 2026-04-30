using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderCallbackInboxOperationalIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProviderCallbackInboxMessages_Provider_CallbackType_CreatedAtUtc",
                schema: "Integration",
                table: "ProviderCallbackInboxMessages",
                columns: new[] { "Provider", "CallbackType", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProviderCallbackInboxMessages_Provider_CallbackType_CreatedAtUtc",
                schema: "Integration",
                table: "ProviderCallbackInboxMessages");
        }
    }
}
