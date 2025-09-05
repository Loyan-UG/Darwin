using Darwin.Domain.Entities.CMS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.CMS
{
    /// <summary>
    /// Ensures (Culture, Slug) is unique among non-deleted pages.
    /// </summary>
    public sealed class PageConfiguration : IEntityTypeConfiguration<Page>
    {
        public void Configure(EntityTypeBuilder<Page> b)
        {
            b.ToTable("Pages");
            b.HasMany(p => p.Translations).WithOne().HasForeignKey(t => t.PageId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class PageTranslationConfiguration : IEntityTypeConfiguration<PageTranslation>
    {
        public void Configure(EntityTypeBuilder<PageTranslation> b)
        {
            b.ToTable("PageTranslations");
            b.Property(x => x.Culture).IsRequired().HasMaxLength(10);
            b.Property(x => x.Slug).IsRequired().HasMaxLength(200);

            // Unique per culture among non-deleted
            b.HasIndex(x => new { x.Culture, x.Slug })
             .IsUnique()
             .HasFilter("[IsDeleted] = 0");
        }
    }
}
