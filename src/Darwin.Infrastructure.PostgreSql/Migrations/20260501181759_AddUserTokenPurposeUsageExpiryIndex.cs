using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTokenPurposeUsageExpiryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserTokens_Purpose_UsedAtUtc_ExpiresAtUtc",
                schema: "Identity",
                table: "UserTokens",
                columns: new[] { "Purpose", "UsedAtUtc", "ExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserTokens_Purpose_UsedAtUtc_ExpiresAtUtc",
                schema: "Identity",
                table: "UserTokens");
        }
    }
}
