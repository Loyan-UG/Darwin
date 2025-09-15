using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Catalog
{
    /// <summary>
    ///     EF Core configuration for <see cref="Brand"/> and <see cref="BrandTranslation"/>:
    ///     sets keys, relationships, indexes (including unique slug and unique (BrandId, Culture)),
    ///     and reasonable constraints for text fields.
    /// </summary>
    public sealed class BrandConfiguration :
        IEntityTypeConfiguration<Brand>,
        IEntityTypeConfiguration<BrandTranslation>
    {
        public void Configure(EntityTypeBuilder<Brand> builder)
        {
            // Table name aligns with existing naming conventions (no extra prefixes)
            builder.ToTable("Brands");

            builder.HasKey(b => b.Id);

            // Slug: globally unique if provided; nullable allowed. Filtered unique index for SQL Server.
            builder.Property(b => b.Slug)
                   .HasMaxLength(256);

            builder.HasIndex(b => b.Slug)
                   .IsUnique()
                   .HasFilter("[Slug] IS NOT NULL");

            // Optional logo reference left as scalar Guid to avoid hard FK to Media at this stage.
            builder.Property(b => b.LogoMediaId);

            // Relationship: Brand (1) -> (N) BrandTranslation
            builder.HasMany(b => b.Translations)
                   .WithOne()
                   .HasForeignKey(t => t.BrandId)
                   .OnDelete(DeleteBehavior.Cascade);
        }

        public void Configure(EntityTypeBuilder<BrandTranslation> builder)
        {
            builder.ToTable("BrandTranslations");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Culture)
                   .IsRequired()
                   .HasMaxLength(16); // e.g., "de-DE", "en-US"

            builder.Property(t => t.Name)
                   .IsRequired()
                   .HasMaxLength(256);

            // DescriptionHtml can be long; store as nvarchar(max) by default (no explicit max length).
            // If you want, you can add .HasColumnType("nvarchar(max)") explicitly.

            // Unique translation per (BrandId, Culture)
            builder.HasIndex(t => new { t.BrandId, t.Culture })
                   .IsUnique();
        }
    }
}
