using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Businesses
{
    /// <summary>
    /// EF Core configuration for <see cref="BusinessMedia"/>.
    /// </summary>
    public sealed class BusinessMediaConfiguration : 
        IEntityTypeConfiguration<BusinessMedia>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<BusinessMedia> builder)
        {
            builder.ToTable("BusinessMedias", schema: "Businesses");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.Property(x => x.BusinessId).IsRequired();
            builder.Property(x => x.BusinessLocationId);

            builder.Property(x => x.Url)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(x => x.Caption)
                .HasMaxLength(500);

            builder.Property(x => x.SortOrder).IsRequired();
            builder.Property(x => x.IsPrimary).IsRequired();

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.BusinessLocationId);
            builder.HasIndex(x => new { x.BusinessId, x.SortOrder });
        }
    }
}
