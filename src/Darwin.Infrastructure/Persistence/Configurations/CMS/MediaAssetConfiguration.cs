using Darwin.Domain.Entities.CMS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.CMS
{
    /// <summary>
    /// EF Core configuration for <see cref="MediaAsset"/>.
    /// Adds length constraints and helpful indexes for deduplication and lookups.
    /// </summary>
    public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
    {
        /// <summary>
        /// Configures <see cref="MediaAsset"/> table mappings, indexes and column sizes.
        /// </summary>
        public void Configure(EntityTypeBuilder<MediaAsset> builder)
        {
            builder.ToTable("MediaAssets", schema: "CMS");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Url).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.Alt).IsRequired().HasMaxLength(300);
            builder.Property(x => x.Title).HasMaxLength(300);
            builder.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(260);
            builder.Property(x => x.SizeBytes).IsRequired();

            builder.Property(x => x.ContentHash).HasMaxLength(128);
            builder.Property(x => x.Width);
            builder.Property(x => x.Height);
            builder.Property(x => x.Role).HasMaxLength(100);

            builder.HasIndex(x => x.ContentHash);
            builder.HasIndex(x => x.Url).HasDatabaseName("IX_MediaAssets_Url");
        }
    }
}
