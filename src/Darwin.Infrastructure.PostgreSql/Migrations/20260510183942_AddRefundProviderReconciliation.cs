using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundProviderReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                schema: "Orders",
                table: "Refunds",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                schema: "Orders",
                table: "Refunds",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderPaymentReference",
                schema: "Orders",
                table: "Refunds",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderRefundReference",
                schema: "Orders",
                table: "Refunds",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderStatus",
                schema: "Orders",
                table: "Refunds",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedAtUtc",
                schema: "Orders",
                table: "Refunds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Orders"."Refunds" AS r
                SET "Provider" = COALESCE(NULLIF(p."Provider", ''), r."Provider"),
                    "ProviderPaymentReference" = COALESCE(p."ProviderPaymentIntentRef", p."ProviderTransactionRef", r."ProviderPaymentReference")
                FROM "Billing"."Payments" AS p
                WHERE r."PaymentId" = p."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureReason",
                schema: "Orders",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "Provider",
                schema: "Orders",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "ProviderPaymentReference",
                schema: "Orders",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "ProviderRefundReference",
                schema: "Orders",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "ProviderStatus",
                schema: "Orders",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "RequestedAtUtc",
                schema: "Orders",
                table: "Refunds");
        }
    }
}
