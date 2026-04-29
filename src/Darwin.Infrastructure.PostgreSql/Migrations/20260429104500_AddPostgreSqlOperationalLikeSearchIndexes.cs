using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DarwinDbContext))]
    [Migration("20260429104500_AddPostgreSqlOperationalLikeSearchIndexes")]
    public partial class AddPostgreSqlOperationalLikeSearchIndexes : Migration
    {
        private static readonly SearchIndex[] SearchIndexes =
        [
            new("Billing", "Payments", "FailureReason", "IX_PG_Payments_FailureReason_Like_Trgm", "\"IsDeleted\" = FALSE AND \"FailureReason\" IS NOT NULL"),
            new("Billing", "Payments", "ProviderTransactionRef", "IX_PG_Payments_ProviderTransactionRef_Like_Trgm", "\"IsDeleted\" = FALSE AND \"ProviderTransactionRef\" IS NOT NULL"),
            new("Billing", "Payments", "ProviderPaymentIntentRef", "IX_PG_Payments_ProviderPaymentIntentRef_Like_Trgm", "\"IsDeleted\" = FALSE AND \"ProviderPaymentIntentRef\" IS NOT NULL"),
            new("Billing", "Payments", "ProviderCheckoutSessionRef", "IX_PG_Payments_ProviderCheckoutSessionRef_Like_Trgm", "\"IsDeleted\" = FALSE AND \"ProviderCheckoutSessionRef\" IS NOT NULL"),

            new("Integration", "WebhookSubscriptions", "EventType", "IX_PG_WebhookSubscriptions_EventType_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Integration", "WebhookSubscriptions", "CallbackUrl", "IX_PG_WebhookSubscriptions_CallbackUrl_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Integration", "WebhookDeliveries", "Status", "IX_PG_WebhookDeliveries_Status_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Integration", "WebhookDeliveries", "IdempotencyKey", "IX_PG_WebhookDeliveries_IdempotencyKey_Like_Trgm", "\"IsDeleted\" = FALSE AND \"IdempotencyKey\" IS NOT NULL"),

            new("Inventory", "InventoryTransactions", "Reason", "IX_PG_InventoryTransactions_Reason_Like_Trgm", "\"IsDeleted\" = FALSE"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""CREATE EXTENSION IF NOT EXISTS pg_trgm;""");

            foreach (var searchIndex in SearchIndexes)
            {
                migrationBuilder.Sql($"""
                    CREATE INDEX IF NOT EXISTS "{searchIndex.Name}"
                    ON "{searchIndex.Schema}"."{searchIndex.Table}"
                    USING GIN ("{searchIndex.Column}" gin_trgm_ops)
                    WHERE {searchIndex.Filter};
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            for (var index = SearchIndexes.Length - 1; index >= 0; index--)
            {
                var searchIndex = SearchIndexes[index];
                migrationBuilder.Sql($"""DROP INDEX IF EXISTS "{searchIndex.Schema}"."{searchIndex.Name}";""");
            }
        }

        private readonly record struct SearchIndex(
            string Schema,
            string Table,
            string Column,
            string Name,
            string Filter);
    }
}
