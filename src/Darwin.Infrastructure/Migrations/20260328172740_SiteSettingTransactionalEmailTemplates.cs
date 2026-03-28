using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SiteSettingTransactionalEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountActivationEmailBodyTemplate",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountActivationEmailSubjectTemplate",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessInvitationEmailBodyTemplate",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessInvitationEmailSubjectTemplate",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetEmailBodyTemplate",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetEmailSubjectTemplate",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountActivationEmailBodyTemplate",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "AccountActivationEmailSubjectTemplate",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "BusinessInvitationEmailBodyTemplate",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "BusinessInvitationEmailSubjectTemplate",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "PasswordResetEmailBodyTemplate",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "PasswordResetEmailSubjectTemplate",
                schema: "Settings",
                table: "SiteSettings");
        }
    }
}
