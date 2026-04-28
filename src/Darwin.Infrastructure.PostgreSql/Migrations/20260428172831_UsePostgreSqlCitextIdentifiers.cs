using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class UsePostgreSqlCitextIdentifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS citext;");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                schema: "Identity",
                table: "Users",
                type: "citext",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedUserName",
                schema: "Identity",
                table: "Users",
                type: "citext",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                schema: "Identity",
                table: "Users",
                type: "citext",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "Identity",
                table: "Users",
                type: "citext",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Pricing",
                table: "TaxCategories",
                type: "citext",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                schema: "Identity",
                table: "Roles",
                type: "citext",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                schema: "Identity",
                table: "Roles",
                type: "citext",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "Pricing",
                table: "Promotions",
                type: "citext",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                schema: "Catalog",
                table: "ProductVariants",
                type: "citext",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "Catalog",
                table: "ProductTranslations",
                type: "citext",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Culture",
                schema: "Catalog",
                table: "ProductTranslations",
                type: "citext",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                schema: "Identity",
                table: "Permissions",
                type: "citext",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "CMS",
                table: "PageTranslations",
                type: "citext",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Culture",
                schema: "CMS",
                table: "PageTranslations",
                type: "citext",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "Catalog",
                table: "CategoryTranslations",
                type: "citext",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Culture",
                schema: "Catalog",
                table: "CategoryTranslations",
                type: "citext",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                schema: "Businesses",
                table: "BusinessInvitations",
                type: "citext",
                maxLength: 320,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320);

            migrationBuilder.AlterColumn<string>(
                name: "Culture",
                schema: "Catalog",
                table: "BrandTranslations",
                type: "citext",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "Catalog",
                table: "Brands",
                type: "citext",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "Billing",
                table: "BillingPlans",
                type: "citext",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                schema: "Identity",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedUserName",
                schema: "Identity",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                schema: "Identity",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "Identity",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Pricing",
                table: "TaxCategories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                schema: "Identity",
                table: "Roles",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                schema: "Identity",
                table: "Roles",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "Pricing",
                table: "Promotions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                schema: "Catalog",
                table: "ProductVariants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "Catalog",
                table: "ProductTranslations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Culture",
                schema: "Catalog",
                table: "ProductTranslations",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                schema: "Identity",
                table: "Permissions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "CMS",
                table: "PageTranslations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Culture",
                schema: "CMS",
                table: "PageTranslations",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "Catalog",
                table: "CategoryTranslations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Culture",
                schema: "Catalog",
                table: "CategoryTranslations",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                schema: "Businesses",
                table: "BusinessInvitations",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 320);

            migrationBuilder.AlterColumn<string>(
                name: "Culture",
                schema: "Catalog",
                table: "BrandTranslations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "Catalog",
                table: "Brands",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "Billing",
                table: "BillingPlans",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128);
        }
    }
}
