using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddPostgreSqlTextJsonValidityChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION public.darwin_is_valid_jsonb(value text)
                RETURNS boolean
                LANGUAGE plpgsql
                IMMUTABLE
                STRICT
                AS $$
                BEGIN
                    PERFORM value::jsonb;
                    RETURN TRUE;
                EXCEPTION WHEN others THEN
                    RETURN FALSE;
                END;
                $$;
                """);

            AddJsonValidityCheck(migrationBuilder, "Billing", "BillingPlans", "FeaturesJson");
            AddJsonValidityCheck(migrationBuilder, "Businesses", "Businesses", "AdminTextOverridesJson", nullable: true);
            AddJsonValidityCheck(migrationBuilder, "CartCheckout", "CartItems", "SelectedAddOnValueIdsJson");
            AddJsonValidityCheck(migrationBuilder, "Integration", "EventLogs", "PropertiesJson");
            AddJsonValidityCheck(migrationBuilder, "Integration", "EventLogs", "UtmSnapshotJson");
            AddJsonValidityCheck(migrationBuilder, "Integration", "ProviderCallbackInboxMessages", "PayloadJson");
            AddJsonValidityCheck(migrationBuilder, "Loyalty", "LoyaltyPrograms", "RulesJson", nullable: true);
            AddJsonValidityCheck(migrationBuilder, "Loyalty", "LoyaltyRewardRedemptions", "MetadataJson", nullable: true);
            AddJsonValidityCheck(migrationBuilder, "Loyalty", "LoyaltyRewardTiers", "MetadataJson", nullable: true);
            AddJsonValidityCheck(migrationBuilder, "Loyalty", "ScanSessions", "SelectedRewardsJson", nullable: true);
            AddJsonValidityCheck(migrationBuilder, "Settings", "SiteSettings", "AdminTextOverridesJson", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropJsonValidityCheck(migrationBuilder, "Settings", "SiteSettings", "AdminTextOverridesJson");
            DropJsonValidityCheck(migrationBuilder, "Loyalty", "ScanSessions", "SelectedRewardsJson");
            DropJsonValidityCheck(migrationBuilder, "Loyalty", "LoyaltyRewardTiers", "MetadataJson");
            DropJsonValidityCheck(migrationBuilder, "Loyalty", "LoyaltyRewardRedemptions", "MetadataJson");
            DropJsonValidityCheck(migrationBuilder, "Loyalty", "LoyaltyPrograms", "RulesJson");
            DropJsonValidityCheck(migrationBuilder, "Integration", "ProviderCallbackInboxMessages", "PayloadJson");
            DropJsonValidityCheck(migrationBuilder, "Integration", "EventLogs", "UtmSnapshotJson");
            DropJsonValidityCheck(migrationBuilder, "Integration", "EventLogs", "PropertiesJson");
            DropJsonValidityCheck(migrationBuilder, "CartCheckout", "CartItems", "SelectedAddOnValueIdsJson");
            DropJsonValidityCheck(migrationBuilder, "Businesses", "Businesses", "AdminTextOverridesJson");
            DropJsonValidityCheck(migrationBuilder, "Billing", "BillingPlans", "FeaturesJson");
            migrationBuilder.Sql("""DROP FUNCTION IF EXISTS public.darwin_is_valid_jsonb(text);""");
        }

        private static void AddJsonValidityCheck(
            MigrationBuilder migrationBuilder,
            string schema,
            string table,
            string column,
            bool nullable = false)
        {
            var constraintName = BuildConstraintName(table, column);
            var expression = nullable
                ? $@"""{column}"" IS NULL OR public.darwin_is_valid_jsonb(""{column}"")"
                : $@"public.darwin_is_valid_jsonb(""{column}"")";

            migrationBuilder.Sql($"""
                ALTER TABLE "{schema}"."{table}"
                ADD CONSTRAINT "{constraintName}"
                CHECK ({expression})
                NOT VALID;
                """);
        }

        private static void DropJsonValidityCheck(
            MigrationBuilder migrationBuilder,
            string schema,
            string table,
            string column)
        {
            migrationBuilder.Sql($"""
                ALTER TABLE "{schema}"."{table}"
                DROP CONSTRAINT IF EXISTS "{BuildConstraintName(table, column)}";
                """);
        }

        private static string BuildConstraintName(string table, string column)
        {
            return $"CK_PG_{table}_{column}_ValidJson";
        }
    }
}
