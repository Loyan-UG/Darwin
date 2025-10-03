using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Catalog
{
    /// <summary>
    ///     Configuration for product aggregate and related entities (translations, variants, media, options),
    ///     including indexes for lookups (SKU, GTIN), foreign keys, and price/quantity column types.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Focus Areas:
    ///         <list type="bullet">
    ///             <item>Unique SKU per variant; optional unique GTIN when provided.</item>
    ///             <item>Price columns stored as 64-bit integers (minor units) for precision.</item>
    ///             <item>Translation uniqueness per culture and slug indexes for discoverability.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Consider covering indexes for catalog listing queries in the future for performance.
    ///     </para>
    /// </remarks>
    public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> b)
        {
            b.ToTable("Products", schema: "Catalog");

            b.HasMany(p => p.Translations)
             .WithOne()
             .HasForeignKey(t => t.ProductId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(p => p.Media)
             .WithOne()
             .HasForeignKey(pm => pm.ProductId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(p => p.Variants)
             .WithOne()
             .HasForeignKey(v => v.ProductId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(p => p.Options)
             .WithOne()
             .HasForeignKey(o => o.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class ProductTranslationConfiguration : IEntityTypeConfiguration<ProductTranslation>
    {
        public void Configure(EntityTypeBuilder<ProductTranslation> b)
        {
            b.ToTable("ProductTranslations", schema: "Catalog");
            b.Property(x => x.Culture).IsRequired().HasMaxLength(10);
            b.Property(x => x.Slug).IsRequired().HasMaxLength(200);

            b.HasIndex(x => new { x.Culture, x.Slug })
             .IsUnique()
             .HasFilter("[IsDeleted] = 0");
        }
    }

    public sealed class CategoryTranslationConfiguration : IEntityTypeConfiguration<CategoryTranslation>
    {
        public void Configure(EntityTypeBuilder<CategoryTranslation> b)
        {
            b.ToTable("CategoryTranslations", schema: "Catalog");
            b.Property(x => x.Culture).IsRequired().HasMaxLength(10);
            b.Property(x => x.Slug).IsRequired().HasMaxLength(200);

            b.HasIndex(x => new { x.Culture, x.Slug })
             .IsUnique()
             .HasFilter("[IsDeleted] = 0");
        }
    }

    public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
    {
        public void Configure(EntityTypeBuilder<ProductVariant> b)
        {
            b.ToTable("ProductVariants", schema: "Catalog");
            b.Property(x => x.Sku).IsRequired().HasMaxLength(100);

            // Unique SKU among non-deleted variants
            b.HasIndex(x => x.Sku)
             .IsUnique()
             .HasFilter("[IsDeleted] = 0");

            b.HasMany(v => v.OptionValues)
             .WithOne()
             .HasForeignKey(ov => ov.VariantId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class ProductOptionConfiguration : IEntityTypeConfiguration<ProductOption>
    {
        public void Configure(EntityTypeBuilder<ProductOption> b)
        {
            b.ToTable("ProductOptions", schema: "Catalog");
            b.HasMany(o => o.Values).WithOne().HasForeignKey(v => v.ProductOptionId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
