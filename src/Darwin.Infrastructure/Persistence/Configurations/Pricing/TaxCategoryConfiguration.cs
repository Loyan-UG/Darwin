using Darwin.Domain.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Pricing
{
    /// <summary>
    /// EF Core mapping for <see cref="TaxCategory"/>.
    /// Matches domain fields Name, VatRate, EffectiveFromUtc, Notes.
    /// </summary>
    public sealed class TaxCategoryConfiguration : IEntityTypeConfiguration<TaxCategory>
    {
        public void Configure(EntityTypeBuilder<TaxCategory> b)
        {
            b.ToTable("TaxCategories", schema: "Pricing");

            b.HasKey(x => x.Id);

            // Columns
            b.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            b.Property(x => x.VatRate)
                .HasPrecision(5, 4); // supports values like 0.1900

            b.Property(x => x.EffectiveFromUtc);
            b.Property(x => x.Notes).HasMaxLength(1000);

            // Useful indexes
            b.HasIndex(x => x.Name);
            b.HasIndex(x => x.EffectiveFromUtc);

            // Soft delete query filter can be global elsewhere; keep config focused here.
        }
    }
}
