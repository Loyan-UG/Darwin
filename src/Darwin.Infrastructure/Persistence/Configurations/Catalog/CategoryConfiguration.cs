using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Catalog
{
    public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>, IEntityTypeConfiguration<CategoryTranslation>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories");
            builder.HasKey(x => x.Id);

            builder.HasMany(x => x.Translations)
                   .WithOne()
                   .HasForeignKey(t => t.CategoryId)
                   .OnDelete(DeleteBehavior.Cascade);
        }

        public void Configure(EntityTypeBuilder<CategoryTranslation> builder)
        {
            builder.ToTable("CategoryTranslations");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Culture).HasMaxLength(10).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
            builder.Property(x => x.Slug).HasMaxLength(200).IsRequired();

            // <== NEW: unique (Culture, Slug) within CategoryTranslations
            builder.HasIndex(x => new { x.Culture, x.Slug }).IsUnique();
        }
    }
}
