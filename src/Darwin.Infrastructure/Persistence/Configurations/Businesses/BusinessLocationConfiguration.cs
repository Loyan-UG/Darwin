using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Businesses
{
    /// <summary>
    /// EF Core configuration for <see cref="BusinessLocation"/>.
    /// </summary>
    public sealed class BusinessLocationConfiguration :
        IEntityTypeConfiguration<BusinessLocation>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<BusinessLocation> builder)
        {
            builder.ToTable("BusinessLocations", schema: "Businesses");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.Property(x => x.BusinessId).IsRequired();

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.AddressLine1)
                .HasMaxLength(250);

            builder.Property(x => x.AddressLine2)
                .HasMaxLength(250);

            builder.Property(x => x.City)
                .HasMaxLength(120);

            builder.Property(x => x.Region)
                .HasMaxLength(120);

            builder.Property(x => x.CountryCode)
                .HasMaxLength(2);

            builder.Property(x => x.PostalCode)
                .HasMaxLength(20);

            builder.Property(x => x.OpeningHoursJson)
                .HasMaxLength(4000);

            builder.Property(x => x.InternalNote)
                .HasMaxLength(2000);

            builder.Property(x => x.IsPrimary)
                .IsRequired();

            // GeoCoordinate value object as owned type.
            builder.OwnsOne(x => x.Coordinate, owned =>
            {
                owned.Property(c => c.Latitude)
                    .HasColumnName("Latitude");

                owned.Property(c => c.Longitude)
                    .HasColumnName("Longitude");

                owned.Property(c => c.AltitudeMeters)
                    .HasColumnName("AltitudeMeters");
            });

            // Indexes
            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => new { x.BusinessId, x.IsPrimary });
        }
    }
}
