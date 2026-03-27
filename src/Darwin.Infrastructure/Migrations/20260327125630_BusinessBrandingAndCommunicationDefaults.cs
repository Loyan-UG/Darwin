using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BusinessBrandingAndCommunicationDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrandDisplayName",
                schema: "Businesses",
                table: "Businesses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandLogoUrl",
                schema: "Businesses",
                table: "Businesses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandPrimaryColorHex",
                schema: "Businesses",
                table: "Businesses",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandSecondaryColorHex",
                schema: "Businesses",
                table: "Businesses",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommunicationReplyToEmail",
                schema: "Businesses",
                table: "Businesses",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommunicationSenderName",
                schema: "Businesses",
                table: "Businesses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportEmail",
                schema: "Businesses",
                table: "Businesses",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrandDisplayName",
                schema: "Businesses",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "BrandLogoUrl",
                schema: "Businesses",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "BrandPrimaryColorHex",
                schema: "Businesses",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "BrandSecondaryColorHex",
                schema: "Businesses",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "CommunicationReplyToEmail",
                schema: "Businesses",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "CommunicationSenderName",
                schema: "Businesses",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "SupportEmail",
                schema: "Businesses",
                table: "Businesses");
        }
    }
}
