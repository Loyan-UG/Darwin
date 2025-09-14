using Darwin.Domain.Entities.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Shipping
{
    /// <summary>
    /// EF Core configuration for <see cref="ShippingMethod"/> and its
    /// related <see cref="ShippingRate"/> entities. Defines table names,
    /// property lengths, indexes and relationships. Also applies global
    /// query filters on <c>IsDeleted</c> inherited from <see cref="BaseEntity"/>.
    /// </summary>
    public sealed class ShippingMethodConfiguration : IEntityTypeConfiguration<ShippingMethod>, IEntityTypeConfiguration<ShippingRate>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<ShippingMethod> builder)
        {
            // Table naming
            builder.ToTable("ShippingMethods");

            // Keys and basic properties
            builder.HasKey(sm => sm.Id);
            builder.Property(sm => sm.Name).HasMaxLength(200).IsRequired();
            builder.Property(sm => sm.Carrier).HasMaxLength(50).IsRequired();
            builder.Property(sm => sm.Service).HasMaxLength(50).IsRequired();
            builder.Property(sm => sm.CountriesCsv).HasMaxLength(255);
            builder.Property(sm => sm.IsActive).IsRequired();

            // Unique constraint across Name/Carrier/Service. Combined index
            // ensures administrators cannot create duplicate methods for the
            // same carrier/service combination. Note: This constraint
            // complements the unique name validators in the application layer.
            builder.HasIndex(sm => new { sm.Name, sm.Carrier, sm.Service }).IsUnique();

            // One-to-many relationship to rate tiers. Cascade delete so that
            // removing a shipping method removes its rates. Use DeleteBehavior
            // Cascade for soft deleted aggregates; the query filter will hide
            // them rather than physically delete.
            builder.HasMany(sm => sm.Rates)
                .WithOne()
                .HasForeignKey(r => r.ShippingMethodId)
                .OnDelete(DeleteBehavior.Cascade);

            // Global query filter defined in Conventions applies here; no
            // additional filter needed.
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<ShippingRate> builder)
        {
            builder.ToTable("ShippingRates");
            builder.HasKey(r => r.Id);
            // Nullable columns don't need explicit configuration. Prices and sort
            // order should always be specified.
            builder.Property(r => r.PriceMinor).IsRequired();
            builder.Property(r => r.SortOrder).IsRequired();

            // Unique sort order per method ensures predictable rate evaluation
            builder.HasIndex(r => new { r.ShippingMethodId, r.SortOrder }).IsUnique();

            // Relationships are configured on the ShippingMethod side; no further
            // configuration is required here.
        }
    }
}