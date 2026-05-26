using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class SiteSettingEmailRoutingBrevo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingEmail",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "billing@loyan.de");

            migrationBuilder.AddColumn<string>(
                name: "BrevoApiKey",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrevoBaseUrl",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "https://api.brevo.com/v3/");

            migrationBuilder.AddColumn<bool>(
                name: "BrevoSandboxMode",
                schema: "Settings",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "BrevoTestRecipientEmail",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrevoWebhookPassword",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrevoWebhookUsername",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoReplyEmail",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "no-reply@loyan.de");

            migrationBuilder.AddColumn<string>(
                name: "SupportEmail",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "support@loyan.de");

            migrationBuilder.AddColumn<string>(
                name: "SystemAdminEmail",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "dev@loyan.de");

            migrationBuilder.AddColumn<string>(
                name: "TransactionalEmailProvider",
                schema: "Settings",
                table: "SiteSettings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Brevo");

            migrationBuilder.AddColumn<string>(
                name: "SenderRole",
                schema: "Integration",
                table: "EmailDispatchOperations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "NoReply");

            migrationBuilder.AddColumn<string>(
                name: "SenderRole",
                schema: "Integration",
                table: "EmailDispatchAudits",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "NoReply");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingEmail",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "BrevoApiKey",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "BrevoBaseUrl",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "BrevoSandboxMode",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "BrevoTestRecipientEmail",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "BrevoWebhookPassword",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "BrevoWebhookUsername",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "NoReplyEmail",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SupportEmail",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SystemAdminEmail",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "TransactionalEmailProvider",
                schema: "Settings",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SenderRole",
                schema: "Integration",
                table: "EmailDispatchOperations");

            migrationBuilder.DropColumn(
                name: "SenderRole",
                schema: "Integration",
                table: "EmailDispatchAudits");
        }
    }
}
