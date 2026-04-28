using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class UsePostgreSqlJsonbDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ConvertToJsonb(migrationBuilder, "Identity", "Users", "LastTouchUtmJson", nullable: false);
            ConvertToJsonb(migrationBuilder, "Identity", "Users", "FirstTouchUtmJson", nullable: false);
            ConvertToJsonb(migrationBuilder, "Identity", "Users", "ExternalIdsJson", nullable: false);
            ConvertToJsonb(migrationBuilder, "Identity", "Users", "ChannelsOptInJson", nullable: false);
            ConvertToJsonb(migrationBuilder, "Billing", "SubscriptionInvoices", "MetadataJson", nullable: true);
            ConvertToJsonb(migrationBuilder, "Billing", "SubscriptionInvoices", "LinesJson", nullable: false);
            ConvertToJsonb(migrationBuilder, "Pricing", "Promotions", "ConditionsJson", nullable: true);
            ConvertToJsonb(migrationBuilder, "Marketing", "Campaigns", "TargetingJson", nullable: false);
            ConvertToJsonb(migrationBuilder, "Marketing", "Campaigns", "PayloadJson", nullable: false);
            ConvertToJsonb(migrationBuilder, "Billing", "BusinessSubscriptions", "MetadataJson", nullable: true);

            CreateJsonbGinIndex(migrationBuilder, "Identity", "Users", "LastTouchUtmJson");
            CreateJsonbGinIndex(migrationBuilder, "Identity", "Users", "FirstTouchUtmJson");
            CreateJsonbGinIndex(migrationBuilder, "Identity", "Users", "ExternalIdsJson");
            CreateJsonbGinIndex(migrationBuilder, "Identity", "Users", "ChannelsOptInJson");
            CreateJsonbGinIndex(migrationBuilder, "Billing", "SubscriptionInvoices", "MetadataJson");
            CreateJsonbGinIndex(migrationBuilder, "Billing", "SubscriptionInvoices", "LinesJson");
            CreateJsonbGinIndex(migrationBuilder, "Pricing", "Promotions", "ConditionsJson");
            CreateJsonbGinIndex(migrationBuilder, "Marketing", "Campaigns", "TargetingJson");
            CreateJsonbGinIndex(migrationBuilder, "Marketing", "Campaigns", "PayloadJson");
            CreateJsonbGinIndex(migrationBuilder, "Billing", "BusinessSubscriptions", "MetadataJson");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropJsonbGinIndex(migrationBuilder, "Billing", "BusinessSubscriptions", "MetadataJson");
            DropJsonbGinIndex(migrationBuilder, "Marketing", "Campaigns", "PayloadJson");
            DropJsonbGinIndex(migrationBuilder, "Marketing", "Campaigns", "TargetingJson");
            DropJsonbGinIndex(migrationBuilder, "Pricing", "Promotions", "ConditionsJson");
            DropJsonbGinIndex(migrationBuilder, "Billing", "SubscriptionInvoices", "LinesJson");
            DropJsonbGinIndex(migrationBuilder, "Billing", "SubscriptionInvoices", "MetadataJson");
            DropJsonbGinIndex(migrationBuilder, "Identity", "Users", "ChannelsOptInJson");
            DropJsonbGinIndex(migrationBuilder, "Identity", "Users", "ExternalIdsJson");
            DropJsonbGinIndex(migrationBuilder, "Identity", "Users", "FirstTouchUtmJson");
            DropJsonbGinIndex(migrationBuilder, "Identity", "Users", "LastTouchUtmJson");

            ConvertJsonbToVarchar(migrationBuilder, "Billing", "BusinessSubscriptions", "MetadataJson", 4000);
            ConvertJsonbToVarchar(migrationBuilder, "Marketing", "Campaigns", "PayloadJson", 8000);
            ConvertJsonbToVarchar(migrationBuilder, "Marketing", "Campaigns", "TargetingJson", 8000);
            ConvertJsonbToVarchar(migrationBuilder, "Pricing", "Promotions", "ConditionsJson", 4000);
            ConvertJsonbToVarchar(migrationBuilder, "Billing", "SubscriptionInvoices", "LinesJson", 4000);
            ConvertJsonbToVarchar(migrationBuilder, "Billing", "SubscriptionInvoices", "MetadataJson", 4000);
            ConvertJsonbToVarchar(migrationBuilder, "Identity", "Users", "ChannelsOptInJson", 4000);
            ConvertJsonbToVarchar(migrationBuilder, "Identity", "Users", "ExternalIdsJson", 4000);
            ConvertJsonbToVarchar(migrationBuilder, "Identity", "Users", "FirstTouchUtmJson", 4000);
            ConvertJsonbToVarchar(migrationBuilder, "Identity", "Users", "LastTouchUtmJson", 4000);
        }

        private static void ConvertToJsonb(
            MigrationBuilder migrationBuilder,
            string schema,
            string table,
            string column,
            bool nullable)
        {
            var nullValue = nullable ? "NULL" : "'{}'::jsonb";
            migrationBuilder.Sql($"""
                ALTER TABLE "{schema}"."{table}"
                ALTER COLUMN "{column}" TYPE jsonb
                USING CASE
                    WHEN "{column}" IS NULL THEN {nullValue}
                    WHEN btrim("{column}") = '' THEN {nullValue}
                    ELSE "{column}"::jsonb
                END;
                """);
        }

        private static void ConvertJsonbToVarchar(
            MigrationBuilder migrationBuilder,
            string schema,
            string table,
            string column,
            int length)
        {
            migrationBuilder.Sql($"""
                ALTER TABLE "{schema}"."{table}"
                ALTER COLUMN "{column}" TYPE character varying({length})
                USING "{column}"::text;
                """);
        }

        private static void CreateJsonbGinIndex(
            MigrationBuilder migrationBuilder,
            string schema,
            string table,
            string column)
        {
            migrationBuilder.Sql($"""
                CREATE INDEX IF NOT EXISTS "IX_PG_{table}_{column}_JsonbGin"
                ON "{schema}"."{table}"
                USING GIN ("{column}" jsonb_path_ops);
                """);
        }

        private static void DropJsonbGinIndex(
            MigrationBuilder migrationBuilder,
            string schema,
            string table,
            string column)
        {
            migrationBuilder.Sql($"""
                DROP INDEX IF EXISTS "{schema}"."IX_PG_{table}_{column}_JsonbGin";
                """);
        }
    }
}
