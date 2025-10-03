using Darwin.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Darwin.Infrastructure.Persistence.Configurations.Inventory
{
    /// <summary>
    /// EF Core configuration for <see cref="InventoryTransaction"/>.
    /// Records stock adjustments with reason and an optional external reference.
    /// </summary>
    public sealed class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
    {
        /// <summary>
        /// Configures column sizes, required fields and useful indexes for reporting.
        /// </summary>
        public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
        {
            builder.ToTable("InventoryTransactions", schema: "Inventory");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.VariantId).IsRequired();
            builder.Property(x => x.QuantityDelta).IsRequired();
            builder.Property(x => x.Reason).IsRequired().HasMaxLength(100);
            builder.Property(x => x.ReferenceId);

            // Query / diagnostics indexes
            builder.HasIndex(x => x.VariantId);
            builder.HasIndex(x => x.CreatedAtUtc);
        }
    }
}
