
using Darwin.Domain.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Pricing
{
    /// <summary>
    /// EF Core mapping for <see cref="PromotionRedemption"/>.
    /// Fields: PromotionId, UserId (nullable), OrderId.
    /// </summary>
    public sealed class PromotionRedemptionConfiguration : IEntityTypeConfiguration<PromotionRedemption>
    {
        public void Configure(EntityTypeBuilder<PromotionRedemption> b)
        {
            b.ToTable("PromotionRedemptions", schema: "Pricing");

            b.HasKey(x => x.Id);

            b.Property(x => x.PromotionId).IsRequired();
            b.Property(x => x.UserId);
            b.Property(x => x.OrderId).IsRequired();

            // Indexes commonly used for reporting/rules
            b.HasIndex(x => x.PromotionId);
            b.HasIndex(x => new { x.PromotionId, x.UserId });
            b.HasIndex(x => x.OrderId)
             .IsUnique(); // one redemption record per order (phase 1 assumption)

            // If later you allow multiple promos per order, drop/adjust the unique constraint.
        }
    }
}

