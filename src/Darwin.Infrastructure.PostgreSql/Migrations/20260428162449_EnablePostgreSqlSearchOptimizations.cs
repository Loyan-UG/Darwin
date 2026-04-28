using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class EnablePostgreSqlSearchOptimizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""CREATE EXTENSION IF NOT EXISTS pg_trgm;""");

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_PG_ProductTranslations_Name_Trgm"
                ON "Catalog"."ProductTranslations"
                USING GIN (lower("Name") gin_trgm_ops)
                WHERE "IsDeleted" = FALSE;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_PG_ProductTranslations_Slug_Trgm"
                ON "Catalog"."ProductTranslations"
                USING GIN (lower("Slug") gin_trgm_ops)
                WHERE "IsDeleted" = FALSE;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_PG_CategoryTranslations_Name_Trgm"
                ON "Catalog"."CategoryTranslations"
                USING GIN (lower("Name") gin_trgm_ops)
                WHERE "IsDeleted" = FALSE;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_PG_CategoryTranslations_Slug_Trgm"
                ON "Catalog"."CategoryTranslations"
                USING GIN (lower("Slug") gin_trgm_ops)
                WHERE "IsDeleted" = FALSE;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_PG_BrandTranslations_Name_Trgm"
                ON "Catalog"."BrandTranslations"
                USING GIN (lower("Name") gin_trgm_ops)
                WHERE "IsDeleted" = FALSE;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_PG_PageTranslations_Title_Trgm"
                ON "CMS"."PageTranslations"
                USING GIN (lower("Title") gin_trgm_ops)
                WHERE "IsDeleted" = FALSE;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_PG_PageTranslations_Slug_Trgm"
                ON "CMS"."PageTranslations"
                USING GIN (lower("Slug") gin_trgm_ops)
                WHERE "IsDeleted" = FALSE;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_PG_Businesses_Name_Trgm"
                ON "Businesses"."Businesses"
                USING GIN (lower("Name") gin_trgm_ops)
                WHERE "IsDeleted" = FALSE;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_PG_Businesses_LegalName_Trgm"
                ON "Businesses"."Businesses"
                USING GIN (lower("LegalName") gin_trgm_ops)
                WHERE "IsDeleted" = FALSE;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_PG_Users_Email_Trgm"
                ON "Identity"."Users"
                USING GIN (lower("Email") gin_trgm_ops)
                WHERE "IsDeleted" = FALSE AND "Email" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Identity"."IX_PG_Users_Email_Trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Businesses"."IX_PG_Businesses_LegalName_Trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Businesses"."IX_PG_Businesses_Name_Trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "CMS"."IX_PG_PageTranslations_Slug_Trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "CMS"."IX_PG_PageTranslations_Title_Trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Catalog"."IX_PG_BrandTranslations_Name_Trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Catalog"."IX_PG_CategoryTranslations_Slug_Trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Catalog"."IX_PG_CategoryTranslations_Name_Trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Catalog"."IX_PG_ProductTranslations_Slug_Trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Catalog"."IX_PG_ProductTranslations_Name_Trgm";""");
        }
    }
}
