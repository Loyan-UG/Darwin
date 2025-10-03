using Darwin.Domain.Entities.CartCheckout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.CartCheckout
{
    /// <summary>
    /// Configures the EF Core mapping for the <see cref="Cart"/> and its nested
    /// <see cref="CartItem"/> entities.  This configuration ensures proper table
    /// names, property lengths, relationships, and concurrency tokens, and sets
    /// up an index on the combination of <c>CartId</c> and <c>VariantId</c> to
    /// prevent duplicate items for the same variant within a cart.  Soft deleted
    /// rows remain in the table but are excluded from queries via the global
    /// query filter defined in the conventions.
    /// </summary>
    public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>, IEntityTypeConfiguration<CartItem>
    {
        /// <summary>
        /// Configures the <see cref="Cart"/> entity.  The table name is
        /// <c>Carts</c>, and a one-to-many relationship to <see cref="CartItem"/>
        /// is established with cascade delete on cart removal.  Concurrency is
        /// handled via a rowversion column.
        /// </summary>
        /// <param name="builder">The type builder for Cart.</param>
        public void Configure(EntityTypeBuilder<Cart> builder)
        {
            builder.ToTable("Carts", schema: "CartCheckout");

            // Primary key
            builder.HasKey(c => c.Id);

            // Optional user identifiers
            builder.Property(c => c.UserId).IsRequired(false);
            builder.Property(c => c.AnonymousId).IsRequired(false);

            // Currency stored as ISO 4217 code; enforce length of 3 characters.
            builder.Property(c => c.Currency)
                   .IsRequired()
                   .HasMaxLength(3);

            // Coupon code is optional; keep length reasonable (e.g. 100 chars)
            builder.Property(c => c.CouponCode)
                   .HasMaxLength(100);

            // Concurrency token using row version for optimistic concurrency control
            builder.Property(c => c.RowVersion)
                   .IsRowVersion();

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.AnonymousId);

            // Relationship: Cart has many CartItems. When a cart is deleted,
            // cascade delete its items.  The soft delete filter on CartItem
            // remains in effect for queries.
            builder.HasMany(c => c.Items)
                   .WithOne()
                   .HasForeignKey(ci => ci.CartId)
                   .OnDelete(DeleteBehavior.Cascade);
        }

        /// <summary>
        /// Configures the <see cref="CartItem"/> entity.  The table name is
        /// <c>CartItems</c>, with a composite unique index on <c>CartId</c> and
        /// <c>VariantId</c> to prevent duplicate variant entries per cart.  A
        /// row version column is used for concurrency, and property lengths and
        /// precision are specified where appropriate.
        /// </summary>
        /// <param name="builder">The type builder for CartItem.</param>
        void IEntityTypeConfiguration<CartItem>.Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.ToTable("CartItems", schema: "CartCheckout");

            builder.HasKey(ci => ci.Id);

            // Foreign key to Cart
            builder.Property(ci => ci.CartId).IsRequired();

            // Variant identifier cannot be empty
            builder.Property(ci => ci.VariantId).IsRequired();

            // Quantity of items must be positive
            builder.Property(ci => ci.Quantity).IsRequired();

            // Monetary values stored as long (minor units)
            builder.Property(ci => ci.UnitPriceNetMinor).IsRequired();

            // VAT rate stored as decimal with precision; ensure non-negative
            builder.Property(ci => ci.VatRate)
                   .HasColumnType("decimal(5, 2)")
                   .IsRequired();

            builder.Property(x => x.SelectedAddOnValueIdsJson)
                .HasMaxLength(4000)
                .IsRequired();

            builder.Property(x => x.AddOnPriceDeltaMinor).HasDefaultValue(0);


            // Concurrency token
            builder.Property(ci => ci.RowVersion)
                   .IsRowVersion();

            // Unique index on (CartId, VariantId) to enforce one entry per variant per cart.
            // We include only non-deleted items to allow re-adding a deleted variant.
            builder.HasIndex(ci => new { ci.CartId, ci.VariantId })
                   .IsUnique();
        }
    }
}