using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.CMS;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Pricing;
using Darwin.Domain.Entities.SEO;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Entities.Shipping;
using Darwin.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Infrastructure.Persistence.Db
{
    /// <summary>
    /// EF Core DbContext containing all aggregates for Darwin. 
    /// Applies global conventions in OnModelCreating via Conventions.Apply().
    /// </summary>
    public sealed class DarwinDbContext : DbContext, IAppDbContext
    {
        public DarwinDbContext(DbContextOptions<DarwinDbContext> options) : base(options) { }

        // CMS
        public DbSet<Menu> Menus => Set<Menu>();
        public DbSet<MenuItem> MenuItems => Set<MenuItem>();
        public DbSet<Page> Pages => Set<Page>();
        public DbSet<PageTranslation> PageTranslations => Set<PageTranslation>();
        public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

        // Catalog
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<CategoryTranslation> CategoryTranslations => Set<CategoryTranslation>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductTranslation> ProductTranslations => Set<ProductTranslation>();
        public DbSet<ProductMedia> ProductMedia => Set<ProductMedia>();
        public DbSet<ProductOption> ProductOptions => Set<ProductOption>();
        public DbSet<ProductOptionValue> ProductOptionValues => Set<ProductOptionValue>();
        public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
        public DbSet<VariantOptionValue> VariantOptionValues => Set<VariantOptionValue>();

        // Inventory
        public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

        // Pricing
        public DbSet<TaxCategory> TaxCategories => Set<TaxCategory>();
        public DbSet<Promotion> Promotions => Set<Promotion>();
        public DbSet<PromotionRedemption> PromotionRedemptions => Set<PromotionRedemption>();

        // Cart/Checkout
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();

        // Orders
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderLine> OrderLines => Set<OrderLine>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Shipment> Shipments => Set<Shipment>();
        public DbSet<ShipmentLine> ShipmentLines => Set<ShipmentLine>();
        public DbSet<Refund> Refunds => Set<Refund>();

        // Shipping
        public DbSet<ShippingMethod> ShippingMethods => Set<ShippingMethod>();
        public DbSet<ShippingRate> ShippingRates => Set<ShippingRate>();

        // Users
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<Address> Addresses => Set<Address>();

        // Integration / SEO / Settings
        public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
        public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
        public DbSet<EventLog> EventLogs => Set<EventLog>();
        public DbSet<RedirectRule> RedirectRules => Set<RedirectRule>();
        public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply global conventions and per-entity configurations
            Conventions.Conventions.Apply(modelBuilder);

            // Apply per-entity configurations where we need explicit indexes/relations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DarwinDbContext).Assembly);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Fill auditing timestamps (UTC) here to keep it consistent in one place.
            var utcNow = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                    entry.Entity.CreatedAtUtc = utcNow;
                if (entry.State == EntityState.Modified)
                    entry.Entity.ModifiedAtUtc = utcNow;
            }
            return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
