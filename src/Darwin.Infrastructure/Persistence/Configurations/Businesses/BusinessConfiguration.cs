using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Businesses
{
    /// <summary>
    /// EF Core configuration for <see cref="Business"/>.
    /// </summary>
    public sealed class BusinessConfiguration : IEntityTypeConfiguration<Business>
    {
        public void Configure(EntityTypeBuilder<Business> b)
        {
            b.ToTable("Businesses", schema: "Businesses");

            b.HasKey(x => x.Id);

            b.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            b.Property(x => x.LegalName).HasMaxLength(200);
            b.Property(x => x.TaxId).HasMaxLength(100);
            b.Property(x => x.ShortDescription).HasMaxLength(1000);
            b.Property(x => x.WebsiteUrl).HasMaxLength(500);
            b.Property(x => x.ContactEmail).HasMaxLength(200);
            b.Property(x => x.ContactPhoneE164).HasMaxLength(30);

            b.Property(x => x.DefaultCurrency)
                .IsRequired()
                .HasMaxLength(3);

            b.Property(x => x.DefaultCulture)
                .IsRequired()
                .HasMaxLength(20);

            b.Property(x => x.Category)
                .IsRequired();

            b.Property(x => x.IsActive)
                .IsRequired();

            b.HasIndex(x => x.Name);
            b.HasIndex(x => x.Category);
            b.HasIndex(x => x.IsActive);

            // Relationships
            b.HasMany(x => x.Locations)
                .WithOne()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.Members)
                .WithOne()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
