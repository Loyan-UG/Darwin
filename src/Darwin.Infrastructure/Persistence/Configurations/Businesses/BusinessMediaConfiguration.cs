using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Businesses
{
    /// <summary>
    /// EF Core configuration for <see cref="BusinessMedia"/>.
    /// </summary>
    public sealed class BusinessMediaConfiguration : IEntityTypeConfiguration<BusinessMedia>
    {
        public void Configure(EntityTypeBuilder<BusinessMedia> b)
        {
            b.ToTable("BusinessMedia", schema: "Businesses");

            b.HasKey(x => x.Id);

            b.Property(x => x.BusinessId).IsRequired();
            b.Property(x => x.BusinessLocationId);

            b.Property(x => x.Url)
                .IsRequired()
                .HasMaxLength(1000);

            b.Property(x => x.Caption).HasMaxLength(400);
            b.Property(x => x.SortOrder).IsRequired();
            b.Property(x => x.IsPrimary).IsRequired();

            b.HasIndex(x => x.BusinessId);
            b.HasIndex(x => new { x.BusinessId, x.IsPrimary });
        }
    }
}
