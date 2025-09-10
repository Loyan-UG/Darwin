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
    ///     Primary EF Core <c>DbContext</c> for the Darwin platform, containing all aggregate roots
    ///     from CMS, Catalog, Inventory, Pricing, Orders, Shipping, Users, SEO/Integration, and Settings.
    ///     This class is defined as <c>partial</c> to separate cross-cutting concerns (e.g., auditing)
    ///     into dedicated files while keeping a single concrete <c>DbContext</c> type for runtime and design-time.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Responsibilities:
    ///         <list type="bullet">
    ///             <item>Expose <c>DbSet&lt;T&gt;</c> properties for domain aggregates.</item>
    ///             <item>Apply global conventions and entity configurations via <c>OnModelCreating</c>.</item>
    ///             <item>Remain free of web-specific or UI logic to keep the Infrastructure layer isolated.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Design:
    ///         <list type="bullet">
    ///             <item>Follows Clean Architecture: Infrastructure persists domain state using EF Core.</item>
    ///             <item>Global conventions (keys, timestamps, soft delete, rowversion) are applied centrally.</item>
    ///             <item>Per-entity configurations (indexes, relations) are discovered from the assembly.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Notes:
    ///         <list type="bullet">
    ///             <item>Auditing logic (Created*/Modified*) is implemented in the partial file <c>DarwinDbContext.Auditing.cs</c>.</item>
    ///             <item>Optimistic concurrency uses SQL Server <c>rowversion</c> mapped as <c>byte[]</c>.</item>
    ///             <item>Soft delete uses the <c>IsDeleted</c> flag; consider global query filters where appropriate.</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public sealed partial class DarwinDbContext : DbContext, IAppDbContext
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
    }
}
