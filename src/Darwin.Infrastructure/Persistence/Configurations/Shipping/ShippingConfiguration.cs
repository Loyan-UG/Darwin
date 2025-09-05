using Darwin.Domain.Entities.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Shipping
{
    public sealed class ShippingMethodConfiguration : IEntityTypeConfiguration<ShippingMethod>
    {
        public void Configure(EntityTypeBuilder<ShippingMethod> b)
        {
            b.ToTable("ShippingMethods");
            b.Property(x => x.Name).IsRequired().HasMaxLength(100);
            b.Property(x => x.Carrier).IsRequired().HasMaxLength(100);
            b.Property(x => x.Service).IsRequired().HasMaxLength(100);
            b.HasMany(x => x.Rates).WithOne().HasForeignKey(r => r.ShippingMethodId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class ShippingRateConfiguration : IEntityTypeConfiguration<ShippingRate>
    {
        public void Configure(EntityTypeBuilder<ShippingRate> b)
        {
            b.ToTable("ShippingRates");
            b.HasIndex(x => new { x.ShippingMethodId, x.SortOrder }).IsUnique().HasFilter("[IsDeleted] = 0");
        }
    }
}
