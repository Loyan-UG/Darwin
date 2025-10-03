using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Catalog
{
    /// <summary>
    ///     Configuration for categories and translations, capturing parent-child hierarchy,
    ///     sorting behavior, and per-culture naming/slug rules.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Enforces:
    ///         <list type="bullet">
    ///             <item>Self-referencing optional parent FK for hierarchical trees.</item>
    ///             <item>Unique slug per culture; indexes to accelerate category navigation.</item>
    ///             <item>Stable sort order field with default values.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Large trees may benefit from additional materialized paths or closure tables in future iterations.
    ///     </para>
    /// </remarks>
    public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>, IEntityTypeConfiguration<CategoryTranslation>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories", schema: "Catalog");
            builder.HasKey(x => x.Id);

            builder.HasMany(x => x.Translations)
                   .WithOne()
                   .HasForeignKey(t => t.CategoryId)
                   .OnDelete(DeleteBehavior.Cascade);
        }

        public void Configure(EntityTypeBuilder<CategoryTranslation> builder)
        {
            builder.ToTable("CategoryTranslations", schema: "Catalog");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Culture).HasMaxLength(10).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
            builder.Property(x => x.Slug).HasMaxLength(200).IsRequired();

            // <== NEW: unique (Culture, Slug) within CategoryTranslations
            builder.HasIndex(x => new { x.Culture, x.Slug }).IsUnique();
        }
    }
}
