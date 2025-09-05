using Darwin.Domain.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Pricing
{
    public sealed class TaxCategoryConfiguration : IEntityTypeConfiguration<TaxCategory>
    {
        public void Configure(EntityTypeBuilder<TaxCategory> b)
        {
            b.ToTable("TaxCategories");
            b.Property(x => x.Name).IsRequired().HasMaxLength(100);
            b.Property(x => x.VatRate).HasPrecision(5, 4); // e.g., 0.1900
        }
    }

    public sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
    {
        public void Configure(EntityTypeBuilder<Promotion> b)
        {
            b.ToTable("Promotions");
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Code).HasMaxLength(100);

            // Unique active coupon code among non-deleted
            b.HasIndex(x => x.Code)
             .IsUnique()
             .HasFilter("[IsDeleted] = 0 AND [Code] IS NOT NULL");
        }
    }
}
