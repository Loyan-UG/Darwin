using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.CMS;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Pricing;
using Darwin.Domain.Entities.SEO;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Entities.Shipping;
using Darwin.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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

        // Businesses
        /// <summary>
        /// Businesses aggregate roots.
        /// </summary>
        public DbSet<Business> Businesses => Set<Business>();

        /// <summary>
        /// Business locations.
        /// </summary>
        public DbSet<BusinessLocation> BusinessLocations => Set<BusinessLocation>();

        /// <summary>
        /// Business media items.
        /// </summary>
        public DbSet<BusinessMedia> BusinessMedia => Set<BusinessMedia>();

        /// <summary>
        /// Business membership links.
        /// </summary>
        public DbSet<BusinessMember> BusinessMembers => Set<BusinessMember>();


        // Integration / SEO / Settings
        public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
        public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
        public DbSet<EventLog> EventLogs => Set<EventLog>();
        public DbSet<RedirectRule> RedirectRules => Set<RedirectRule>();
        public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();


        /// <summary>
        /// Configures EF Core model:
        /// - Applies all IEntityTypeConfiguration<T> in this assembly.
        /// - Enforces a global query filter for soft-delete (IsDeleted = false) on all BaseEntity types.
        /// - Maps byte[] RowVersion properties as concurrency tokens wherever present.
        /// - Hooks optional global converters (e.g., secret protection) if available.
        /// </summary>
        /// <param name="modelBuilder">Model builder provided by EF Core.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1) Apply all IEntityTypeConfiguration<T> from Infrastructure assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DarwinDbContext).Assembly);

            // 2) Global soft-delete filter for entities that have a bool IsDeleted property
            ApplySoftDeleteQueryFilters(modelBuilder);

            // 3) Mark "RowVersion" (byte[]) as rowversion/concurrency token when present
            ApplyRowVersionConcurrency(modelBuilder);

            // 5) Optional: register global value converters/protectors (only if class exists in project).
            //    If you are using the SecretProtectionConverterFactory we created earlier, uncomment:
            //SecretProtectionConverterFactory.Apply(modelBuilder);
        }

        /// <summary>
        /// Scans all entity types and applies a global query filter (IsDeleted == false)
        /// when a boolean IsDeleted property exists.
        /// </summary>
        private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var isDeleted = entityType.FindProperty("IsDeleted");
                if (isDeleted is null || isDeleted.ClrType != typeof(bool)) continue;

                // e => EF.Property<bool>(e, "IsDeleted") == false
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var left = Expression.Call(
                    typeof(EF), nameof(EF.Property), new[] { typeof(bool) },
                    parameter, Expression.Constant("IsDeleted"));
                var body = Expression.Equal(left, Expression.Constant(false));
                var lambda = Expression.Lambda(body, parameter);

                entityType.SetQueryFilter(lambda);
            }
        }

        /// <summary>
        /// Configures byte[] RowVersion properties to be proper rowversion concurrency tokens.
        /// </summary>
        private static void ApplyRowVersionConcurrency(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var rv = entityType.FindProperty("RowVersion");
                if (rv is null || rv.ClrType != typeof(byte[])) continue;

                // Use the non-generic builder to avoid reflection gymnastics
                var builder = modelBuilder.Entity(entityType.ClrType);
                builder.Property<byte[]>("RowVersion").IsRowVersion();
                // (.IsRowVersion() sets concurrency token and value generation appropriately)
            }
        }
    }
}
