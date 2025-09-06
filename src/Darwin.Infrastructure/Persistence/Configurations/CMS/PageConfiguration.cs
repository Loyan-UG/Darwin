using Darwin.Domain.Entities.CMS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.CMS
{
    /// <summary>
    /// EF Core configuration for CMS Page and PageTranslation entities.
    /// </summary>
    public sealed class PageConfiguration : IEntityTypeConfiguration<Page>, IEntityTypeConfiguration<PageTranslation>
    {
        public void Configure(EntityTypeBuilder<Page> builder)
        {
            builder.ToTable("Cms_Pages");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status)
                   .IsRequired();

            builder.Property(x => x.PublishStartUtc);
            builder.Property(x => x.PublishEndUtc);

            builder.HasMany(x => x.Translations)
                   .WithOne()
                   .HasForeignKey(t => t.PageId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Common base fields (if you have global conventions, this may be redundant)
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.ModifiedAtUtc);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.Property(x => x.RowVersion).IsRowVersion();
        }

        public void Configure(EntityTypeBuilder<PageTranslation> builder)
        {
            builder.ToTable("Cms_PageTranslations");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Culture)
                   .HasMaxLength(10)
                   .IsRequired();

            builder.Property(x => x.Title)
                   .HasMaxLength(300)
                   .IsRequired();

            builder.Property(x => x.Slug)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.Property(x => x.MetaTitle).HasMaxLength(300);
            builder.Property(x => x.MetaDescription).HasMaxLength(500);

            // ContentHtml per-culture (long text)
            builder.Property(x => x.ContentHtml)
                   .HasColumnType("nvarchar(max)")  // SQL Server
                   .IsRequired();

            // Unique slug per (Page, Culture)
            builder.HasIndex(x => new { x.PageId, x.Culture, x.Slug })
                   .IsUnique();

            // Base
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.ModifiedAtUtc);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.Property(x => x.RowVersion).IsRowVersion();
        }
    }
}
