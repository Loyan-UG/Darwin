using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SiteSettingTransactionalCommunicationPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommunicationTestInboxEmail",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionalEmailSubjectPrefix",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommunicationTestInboxEmail",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "TransactionalEmailSubjectPrefix",
                schema: "Settings",
                table: "SiteSettings");
        }
    }
}
