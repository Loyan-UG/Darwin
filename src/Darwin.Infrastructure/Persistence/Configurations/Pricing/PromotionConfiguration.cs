using Darwin.Domain.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Pricing
{
    /// <summary>
    /// EF Core mapping for <see cref="Promotion"/>.
    /// Fields: Name, Code, Type, AmountMinor, Percent, Currency, MinSubtotalNetMinor,
    /// ConditionsJson, StartsAtUtc, EndsAtUtc, MaxRedemptions, PerCustomerLimit, IsActive.
    /// </summary>
    public sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
    {
        public void Configure(EntityTypeBuilder<Promotion> b)
        {
            b.ToTable("Promotions", schema: "Pricing");

            b.HasKey(x => x.Id);

            // Columns
            b.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            b.Property(x => x.Code)
                .HasMaxLength(100);

            b.Property(x => x.Type)
                .IsRequired();

            b.Property(x => x.AmountMinor);
            b.Property(x => x.Percent).HasPrecision(5, 2); // 0..100.00

            b.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3);

            b.Property(x => x.MinSubtotalNetMinor);

            b.Property(x => x.ConditionsJson)
                .HasMaxLength(4000);

            b.Property(x => x.StartsAtUtc);
            b.Property(x => x.EndsAtUtc);

            b.Property(x => x.MaxRedemptions);
            b.Property(x => x.PerCustomerLimit);

            b.Property(x => x.IsActive)
                .HasDefaultValue(true);

            // Indexes for lookups & constraints
            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => new { x.StartsAtUtc, x.EndsAtUtc });

            // Uniqueness of Code among *active* non-deleted promotions (SQL Server filtered index).
            // If you need cross-DB portability, enforce via Application logic instead.
            b.HasIndex(x => x.Code)
                .HasDatabaseName("UX_Promotions_Code_Active")
#if NET8_0_OR_GREATER
                .HasFilter("[Code] IS NOT NULL AND [IsActive] = 1 AND [IsDeleted] = 0");
#else
                .HasFilter("[Code] IS NOT NULL AND [IsActive] = 1 AND [IsDeleted] = 0");
#endif
        }
    }
}
