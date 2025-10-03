using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Catalog
{
    /// <summary>
    /// EF Core configuration for <see cref="AddOnGroup"/>.
    /// Defines core constraints (currency length, selection bounds) and indexes.
    /// </summary>
    public sealed class AddOnGroupConfiguration : IEntityTypeConfiguration<AddOnGroup>
    {
        /// <summary>Configures the AddOnGroup table and relationships.</summary>
        public void Configure(EntityTypeBuilder<AddOnGroup> builder)
        {
            builder.ToTable("AddOnGroups", schema: "Catalog");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();

            builder.Property(x => x.MinSelections).HasDefaultValue(0);
            builder.Property(x => x.IsActive).HasDefaultValue(true);

            builder.HasIndex(x => x.IsGlobal);
            builder.HasIndex(x => x.IsActive);

            builder
                .HasMany(x => x.Options)
                .WithOne()
                .HasForeignKey(o => o.AddOnGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>EF configuration for <see cref="AddOnOption"/>.</summary>
    public sealed class AddOnOptionConfiguration : IEntityTypeConfiguration<AddOnOption>
    {
        /// <summary>Configures AddOnOption columns and indexes.</summary>
        public void Configure(EntityTypeBuilder<AddOnOption> builder)
        {
            builder.ToTable("AddOnOptions", schema: "Catalog");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Label).HasMaxLength(200).IsRequired();
            builder.Property(x => x.SortOrder).HasDefaultValue(0);

            builder.HasIndex(x => new { x.AddOnGroupId, x.SortOrder });
        }
    }

    /// <summary>EF configuration for <see cref="AddOnOptionValue"/>.</summary>
    public sealed class AddOnOptionValueConfiguration : IEntityTypeConfiguration<AddOnOptionValue>
    {
        /// <summary>Configures value sizing, flags and helpful indexes.</summary>
        public void Configure(EntityTypeBuilder<AddOnOptionValue> builder)
        {
            builder.ToTable("AddOnOptionValues", schema: "Catalog");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Label).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Hint).HasMaxLength(200);
            builder.Property(x => x.PriceDeltaMinor).IsRequired();
            builder.Property(x => x.SortOrder).HasDefaultValue(0);
            builder.Property(x => x.IsActive).HasDefaultValue(true);

            builder.HasIndex(x => new { x.AddOnOptionId, x.SortOrder });
            builder.HasIndex(x => x.IsActive);
        }
    }

    /// <summary>Join entity configuration for <see cref="AddOnGroupProduct"/>.</summary>
    public sealed class AddOnGroupProductConfiguration : IEntityTypeConfiguration<AddOnGroupProduct>
    {
        /// <summary>Enforces uniqueness per (Group, Product).</summary>
        public void Configure(EntityTypeBuilder<AddOnGroupProduct> builder)
        {
            builder.ToTable("AddOnGroupProducts", schema: "Catalog");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.AddOnGroupId, x.ProductId }).IsUnique();
        }
    }

    /// <summary>Join entity configuration for <see cref="AddOnGroupCategory"/>.</summary>
    public sealed class AddOnGroupCategoryConfiguration : IEntityTypeConfiguration<AddOnGroupCategory>
    {
        /// <summary>Enforces uniqueness per (Group, Category).</summary>
        public void Configure(EntityTypeBuilder<AddOnGroupCategory> builder)
        {
            builder.ToTable("AddOnGroupCategories", schema: "Catalog");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.AddOnGroupId, x.CategoryId }).IsUnique();
        }
    }

    /// <summary>Join entity configuration for <see cref="AddOnGroupBrand"/>.</summary>
    public sealed class AddOnGroupBrandConfiguration : IEntityTypeConfiguration<AddOnGroupBrand>
    {
        /// <summary>Enforces uniqueness per (Group, Brand).</summary>
        public void Configure(EntityTypeBuilder<AddOnGroupBrand> builder)
        {
            builder.ToTable("AddOnGroupBrands", schema: "Catalog");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.AddOnGroupId, x.BrandId }).IsUnique();
        }
    }
}
