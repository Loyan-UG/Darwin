using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Businesses
{
    /// <summary>
    /// EF Core configuration for <see cref="BusinessLocation"/>.
    /// </summary>
    public sealed class BusinessLocationConfiguration : IEntityTypeConfiguration<BusinessLocation>
    {
        public void Configure(EntityTypeBuilder<BusinessLocation> b)
        {
            b.ToTable("BusinessLocations", schema: "Businesses");

            b.HasKey(x => x.Id);

            b.Property(x => x.BusinessId).IsRequired();

            b.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            b.Property(x => x.AddressLine1).HasMaxLength(200);
            b.Property(x => x.AddressLine2).HasMaxLength(200);
            b.Property(x => x.City).HasMaxLength(100);
            b.Property(x => x.Region).HasMaxLength(100);
            b.Property(x => x.CountryCode).HasMaxLength(2);
            b.Property(x => x.PostalCode).HasMaxLength(20);
            b.Property(x => x.OpeningHoursJson).HasMaxLength(4000);
            b.Property(x => x.InternalNote).HasMaxLength(2000);

            b.HasIndex(x => x.BusinessId);
            b.HasIndex(x => new { x.BusinessId, x.IsPrimary });

            // Coordinate is a value object. Converters/configs are handled by global conventions if present.
        }
    }
}
