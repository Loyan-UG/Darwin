using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddPostgreSqlTextJsonSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""CREATE EXTENSION IF NOT EXISTS pg_trgm;""");

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_PG_EventLogs_PropertiesJson_Trgm"
                ON "Integration"."EventLogs"
                USING GIN ("PropertiesJson" gin_trgm_ops)
                WHERE "PropertiesJson" IS NOT NULL AND "PropertiesJson" <> '{}';
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_PG_ProviderCallbackInboxMessages_PayloadJson_Trgm"
                ON "Integration"."ProviderCallbackInboxMessages"
                USING GIN ("PayloadJson" gin_trgm_ops)
                WHERE "PayloadJson" IS NOT NULL AND "PayloadJson" <> '';
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_PG_Businesses_AdminTextOverridesJson_Trgm"
                ON "Businesses"."Businesses"
                USING GIN ("AdminTextOverridesJson" gin_trgm_ops)
                WHERE "IsDeleted" = FALSE
                  AND "AdminTextOverridesJson" IS NOT NULL
                  AND "AdminTextOverridesJson" <> '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Businesses"."IX_PG_Businesses_AdminTextOverridesJson_Trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Integration"."IX_PG_ProviderCallbackInboxMessages_PayloadJson_Trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Integration"."IX_PG_EventLogs_PropertiesJson_Trgm";""");
        }
    }
}
