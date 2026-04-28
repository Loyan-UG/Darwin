using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class CommunicationAuditDeliveryMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderCheckoutSessionRef",
                schema: "Billing",
                table: "Payments",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderPaymentIntentRef",
                schema: "Billing",
                table: "Payments",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationKey",
                table: "EmailDispatchAudits",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntendedRecipientEmail",
                table: "EmailDispatchAudits",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderMessageId",
                table: "EmailDispatchAudits",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateKey",
                table: "EmailDispatchAudits",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationKey",
                table: "ChannelDispatchAudits",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntendedRecipientAddress",
                table: "ChannelDispatchAudits",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderMessageId",
                table: "ChannelDispatchAudits",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateKey",
                table: "ChannelDispatchAudits",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderCheckoutSessionRef",
                schema: "Billing",
                table: "Payments",
                column: "ProviderCheckoutSessionRef");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderPaymentIntentRef",
                schema: "Billing",
                table: "Payments",
                column: "ProviderPaymentIntentRef");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDispatchAudits_CorrelationKey",
                table: "EmailDispatchAudits",
                column: "CorrelationKey");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDispatchAudits_IntendedRecipientEmail",
                table: "EmailDispatchAudits",
                column: "IntendedRecipientEmail");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchAudits_CorrelationKey",
                table: "ChannelDispatchAudits",
                column: "CorrelationKey");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchAudits_IntendedRecipientAddress",
                table: "ChannelDispatchAudits",
                column: "IntendedRecipientAddress");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_ProviderCheckoutSessionRef",
                schema: "Billing",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ProviderPaymentIntentRef",
                schema: "Billing",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_EmailDispatchAudits_CorrelationKey",
                table: "EmailDispatchAudits");

            migrationBuilder.DropIndex(
                name: "IX_EmailDispatchAudits_IntendedRecipientEmail",
                table: "EmailDispatchAudits");

            migrationBuilder.DropIndex(
                name: "IX_ChannelDispatchAudits_CorrelationKey",
                table: "ChannelDispatchAudits");

            migrationBuilder.DropIndex(
                name: "IX_ChannelDispatchAudits_IntendedRecipientAddress",
                table: "ChannelDispatchAudits");

            migrationBuilder.DropColumn(
                name: "ProviderCheckoutSessionRef",
                schema: "Billing",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderPaymentIntentRef",
                schema: "Billing",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CorrelationKey",
                table: "EmailDispatchAudits");

            migrationBuilder.DropColumn(
                name: "IntendedRecipientEmail",
                table: "EmailDispatchAudits");

            migrationBuilder.DropColumn(
                name: "ProviderMessageId",
                table: "EmailDispatchAudits");

            migrationBuilder.DropColumn(
                name: "TemplateKey",
                table: "EmailDispatchAudits");

            migrationBuilder.DropColumn(
                name: "CorrelationKey",
                table: "ChannelDispatchAudits");

            migrationBuilder.DropColumn(
                name: "IntendedRecipientAddress",
                table: "ChannelDispatchAudits");

            migrationBuilder.DropColumn(
                name: "ProviderMessageId",
                table: "ChannelDispatchAudits");

            migrationBuilder.DropColumn(
                name: "TemplateKey",
                table: "ChannelDispatchAudits");
        }
    }
}
