using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Businesses
{
    /// <summary>
    /// EF Core configuration for Businesses module entities.
    /// Uses Darwin global conventions for audit, soft-delete, and rowversion.
    /// Entity-specific rules (table names, lengths, relations, indexes) are defined here.
    /// </summary>
    public sealed class BusinessConfiguration :
        IEntityTypeConfiguration<Business>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Business> builder)
        {
            builder.ToTable("Businesses", schema: "Businesses");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.LegalName)
                .HasMaxLength(250);

            builder.Property(x => x.TaxId)
                .HasMaxLength(64);

            builder.Property(x => x.ShortDescription)
                .HasMaxLength(1000);

            builder.Property(x => x.WebsiteUrl)
                .HasMaxLength(500);

            builder.Property(x => x.ContactEmail)
                .HasMaxLength(256);

            builder.Property(x => x.ContactPhoneE164)
                .HasMaxLength(32);

            builder.Property(x => x.DefaultCurrency)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(x => x.DefaultCulture)
                .IsRequired()
                .HasMaxLength(16);

            builder.Property(x => x.Category)
                .IsRequired();

            builder.Property(x => x.IsActive)
                .IsRequired();

            // Helpful lookups
            builder.HasIndex(x => x.Name);
            builder.HasIndex(x => x.Category);
            builder.HasIndex(x => x.IsActive);

            // Relationships
            builder.HasMany(x => x.Members)
                .WithOne()
                .HasForeignKey(m => m.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Locations)
                .WithOne()
                .HasForeignKey(l => l.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
