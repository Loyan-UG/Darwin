using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class SiteSettingMultiChannelTestTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommunicationTestSmsRecipientE164",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommunicationTestWhatsAppRecipientE164",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommunicationTestSmsRecipientE164",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CommunicationTestWhatsAppRecipientE164",
                schema: "Settings",
                table: "SiteSettings");
        }
    }
}
