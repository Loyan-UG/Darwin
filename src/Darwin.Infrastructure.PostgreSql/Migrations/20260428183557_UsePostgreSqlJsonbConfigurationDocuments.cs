using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class UsePostgreSqlJsonbConfigurationDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CreateTryParseJsonbFunction(migrationBuilder);

            ConvertToJsonb(migrationBuilder, "Identity", "UserEngagementSnapshots", "SnapshotJson", "'{}'::jsonb");
            ConvertToJsonb(migrationBuilder, "Settings", "SiteSettings", "SmsExtraSettingsJson", "NULL");
            ConvertToJsonb(migrationBuilder, "Settings", "SiteSettings", "OpenGraphDefaultsJson", "NULL");
            ConvertToJsonb(migrationBuilder, "Settings", "SiteSettings", "NumberFormattingOverridesJson", "NULL");
            ConvertToJsonb(migrationBuilder, "Settings", "SiteSettings", "MeasurementSettingsJson", "NULL");
            ConvertToJsonb(migrationBuilder, "Settings", "SiteSettings", "FeatureFlagsJson", "NULL");
            ConvertToJsonb(migrationBuilder, "Orders", "Orders", "ShippingAddressJson", "'{}'::jsonb");
            ConvertToJsonb(migrationBuilder, "Orders", "Orders", "BillingAddressJson", "'{}'::jsonb");
            ConvertToJsonb(migrationBuilder, "Orders", "OrderLines", "AddOnValueIdsJson", "'[]'::jsonb");
            ConvertToJsonb(migrationBuilder, "Businesses", "BusinessLocations", "OpeningHoursJson", "NULL");
            ConvertToJsonb(migrationBuilder, "Integration", "AnalyticsExportJobs", "ParametersJson", "'{}'::jsonb");

            CreateJsonbGinIndex(migrationBuilder, "Identity", "UserEngagementSnapshots", "SnapshotJson");
            CreateJsonbGinIndex(migrationBuilder, "Settings", "SiteSettings", "FeatureFlagsJson");
            CreateJsonbGinIndex(migrationBuilder, "Businesses", "BusinessLocations", "OpeningHoursJson");
            CreateJsonbGinIndex(migrationBuilder, "Integration", "AnalyticsExportJobs", "ParametersJson");

            DropTryParseJsonbFunction(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropJsonbGinIndex(migrationBuilder, "Integration", "AnalyticsExportJobs", "ParametersJson");
            DropJsonbGinIndex(migrationBuilder, "Businesses", "BusinessLocations", "OpeningHoursJson");
            DropJsonbGinIndex(migrationBuilder, "Settings", "SiteSettings", "FeatureFlagsJson");
            DropJsonbGinIndex(migrationBuilder, "Identity", "UserEngagementSnapshots", "SnapshotJson");

            ConvertJsonbToText(migrationBuilder, "Integration", "AnalyticsExportJobs", "ParametersJson", "character varying(8000)");
            ConvertJsonbToText(migrationBuilder, "Businesses", "BusinessLocations", "OpeningHoursJson", "character varying(4000)");
            ConvertJsonbToText(migrationBuilder, "Orders", "OrderLines", "AddOnValueIdsJson", "text");
            ConvertJsonbToText(migrationBuilder, "Orders", "Orders", "BillingAddressJson", "text");
            ConvertJsonbToText(migrationBuilder, "Orders", "Orders", "ShippingAddressJson", "text");
            ConvertJsonbToText(migrationBuilder, "Settings", "SiteSettings", "FeatureFlagsJson", "character varying(2000)");
            ConvertJsonbToText(migrationBuilder, "Settings", "SiteSettings", "MeasurementSettingsJson", "character varying(2000)");
            ConvertJsonbToText(migrationBuilder, "Settings", "SiteSettings", "NumberFormattingOverridesJson", "character varying(2000)");
            ConvertJsonbToText(migrationBuilder, "Settings", "SiteSettings", "OpenGraphDefaultsJson", "character varying(2000)");
            ConvertJsonbToText(migrationBuilder, "Settings", "SiteSettings", "SmsExtraSettingsJson", "character varying(2000)");
            ConvertJsonbToText(migrationBuilder, "Identity", "UserEngagementSnapshots", "SnapshotJson", "character varying(8000)");

            DropTryParseJsonbFunction(migrationBuilder);
        }

        private static void CreateTryParseJsonbFunction(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION public.darwin_try_parse_jsonb(value text, fallback jsonb)
                RETURNS jsonb
                LANGUAGE plpgsql
                IMMUTABLE
                AS $$
                BEGIN
                    IF value IS NULL OR btrim(value) = '' THEN
                        RETURN fallback;
                    END IF;

                    RETURN value::jsonb;
                EXCEPTION WHEN others THEN
                    RETURN fallback;
                END;
                $$;
                """);
        }

        private static void DropTryParseJsonbFunction(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP FUNCTION IF EXISTS public.darwin_try_parse_jsonb(text, jsonb);""");
        }

        private static void ConvertToJsonb(
            MigrationBuilder migrationBuilder,
            string schema,
            string table,
            string column,
            string emptyValue)
        {
            migrationBuilder.Sql($"""
                ALTER TABLE "{schema}"."{table}"
                ALTER COLUMN "{column}" TYPE jsonb
                USING public.darwin_try_parse_jsonb("{column}", {emptyValue});
                """);
        }

        private static void ConvertJsonbToText(
            MigrationBuilder migrationBuilder,
            string schema,
            string table,
            string column,
            string targetType)
        {
            migrationBuilder.Sql($"""
                ALTER TABLE "{schema}"."{table}"
                ALTER COLUMN "{column}" TYPE {targetType}
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
