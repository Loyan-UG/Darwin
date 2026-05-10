using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.CMS;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Marketing;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Pricing;
using Darwin.Domain.Entities.SEO;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Entities.Shipping;
using Darwin.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
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
        public DbSet<AddOnGroupTranslation> AddOnGroupTranslations => Set<AddOnGroupTranslation>();
        public DbSet<AddOnOptionTranslation> AddOnOptionTranslations => Set<AddOnOptionTranslation>();
        public DbSet<AddOnOptionValueTranslation> AddOnOptionValueTranslations => Set<AddOnOptionValueTranslation>();

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
        public DbSet<Shipment> Shipments => Set<Shipment>();
        public DbSet<ShipmentCarrierEvent> ShipmentCarrierEvents => Set<ShipmentCarrierEvent>();
        public DbSet<ShipmentLine> ShipmentLines => Set<ShipmentLine>();
        public DbSet<Refund> Refunds => Set<Refund>();

        // Shipping
        public DbSet<ShippingMethod> ShippingMethods => Set<ShippingMethod>();
        public DbSet<ShippingRate> ShippingRates => Set<ShippingRate>();


        // Integration / SEO / Settings / Analytics
        public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
        public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
        public DbSet<EventLog> EventLogs => Set<EventLog>();
        public DbSet<EmailDispatchOperation> EmailDispatchOperations => Set<EmailDispatchOperation>();
        public DbSet<ChannelDispatchOperation> ChannelDispatchOperations => Set<ChannelDispatchOperation>();
        public DbSet<ProviderCallbackInboxMessage> ProviderCallbackInboxMessages => Set<ProviderCallbackInboxMessage>();
        public DbSet<ShipmentProviderOperation> ShipmentProviderOperations => Set<ShipmentProviderOperation>();
        public DbSet<EmailDispatchAudit> EmailDispatchAudits => Set<EmailDispatchAudit>();
        public DbSet<ChannelDispatchAudit> ChannelDispatchAudits => Set<ChannelDispatchAudit>();
        public DbSet<RedirectRule> RedirectRules => Set<RedirectRule>();
        public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

        /// <summary>
        /// Export jobs requested by business users.
        /// </summary>
        public DbSet<AnalyticsExportJob> AnalyticsExportJobs => Set<AnalyticsExportJob>();

        /// <summary>
        /// Output files produced by <see cref="AnalyticsExportJob"/>.
        /// </summary>
        public DbSet<AnalyticsExportFile> AnalyticsExportFiles => Set<AnalyticsExportFile>();


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

            // 2) Keep decimal storage explicit across providers; entity-specific configurations can still override this.
            ApplyDecimalPrecisionFallback(modelBuilder);

            // 3) Global soft-delete filter for entities that have a bool IsDeleted property
            ApplySoftDeleteQueryFilters(modelBuilder);

            // 4) Mark "RowVersion" (byte[]) as provider-appropriate concurrency token when present.
            ApplyRowVersionConcurrency(modelBuilder);

            // 5) Keep explicit SQL Server filtered indexes usable when the active provider is PostgreSQL.
            NormalizeProviderSpecificIndexFilters(modelBuilder);

            // 6) Use PostgreSQL-native case-insensitive text for stable identifiers.
            ApplyPostgreSqlCitextIdentifiers(modelBuilder);

            // 7) Use PostgreSQL jsonb for operational JSON documents that benefit from GIN indexes and JSON operators.
            ApplyPostgreSqlJsonbColumns(modelBuilder);

            // 8) Optional: register global value converters/protectors (only if class exists in project).
            //    If you are using the SecretProtectionConverterFactory we created earlier, uncomment:
            //SecretProtectionConverterFactory.Apply(modelBuilder);
        }

        /// <summary>
        /// Applies a safe provider-neutral precision to decimal properties that do not have an explicit precision.
        /// Most money in Darwin is stored as minor-unit integers; this protects future decimal ratios/rates from provider defaults.
        /// </summary>
        private static void ApplyDecimalPrecisionFallback(ModelBuilder modelBuilder)
        {
            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entityType => entityType.GetProperties()))
            {
                var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (clrType != typeof(decimal) || property.GetPrecision().HasValue)
                {
                    continue;
                }

                property.SetPrecision(18);
                property.SetScale(4);
            }
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
        /// Configures byte[] RowVersion properties to be provider-appropriate concurrency tokens.
        /// </summary>
        private void ApplyRowVersionConcurrency(ModelBuilder modelBuilder)
        {
            var providerName = Database.ProviderName;
            var isClientManagedRowVersion =
                providerName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true ||
                IsInMemoryProvider();

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var rv = entityType.FindProperty("RowVersion");
                if (rv is null || rv.ClrType != typeof(byte[])) continue;

                // Use the non-generic builder to avoid reflection gymnastics
                var builder = modelBuilder.Entity(entityType.ClrType);
                var property = builder.Property<byte[]>("RowVersion");
                if (isClientManagedRowVersion)
                {
                    property.IsConcurrencyToken().ValueGeneratedNever().IsRequired();
                }
                else
                {
                    property.IsRowVersion();
                    // (.IsRowVersion() sets concurrency token and value generation appropriately)
                }
            }
        }

        private bool IsInMemoryProvider()
        {
            return this.GetService<IDbContextOptions>()
                .Extensions
                .Any(extension => extension.GetType().Name.Contains("InMemory", StringComparison.OrdinalIgnoreCase));
        }

        private void NormalizeProviderSpecificIndexFilters(ModelBuilder modelBuilder)
        {
            var providerName = Database.ProviderName;
            if (providerName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) != true)
            {
                return;
            }

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var index in entityType.GetIndexes())
                {
                    var filter = index.GetFilter();
                    if (string.IsNullOrWhiteSpace(filter))
                    {
                        continue;
                    }

                    ((IMutableIndex)index).SetFilter(ConvertSqlServerFilterToPostgreSql(filter));
                }
            }
        }

        private static string ConvertSqlServerFilterToPostgreSql(string filter)
        {
            var converted = Regex.Replace(
                filter,
                @"\[([A-Za-z_][A-Za-z0-9_]*)\]",
                static match => $"\"{match.Groups[1].Value}\"",
                RegexOptions.CultureInvariant);

            converted = Regex.Replace(
                converted,
                @"(""[A-Za-z_][A-Za-z0-9_]*"")\s*=\s*0",
                "$1 = FALSE",
                RegexOptions.CultureInvariant);

            converted = Regex.Replace(
                converted,
                @"(""[A-Za-z_][A-Za-z0-9_]*"")\s*=\s*1",
                "$1 = TRUE",
                RegexOptions.CultureInvariant);

            converted = Regex.Replace(
                converted,
                @"\bN'",
                "'",
                RegexOptions.CultureInvariant);

            return converted;
        }

        private void ApplyPostgreSqlCitextIdentifiers(ModelBuilder modelBuilder)
        {
            if (Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) != true)
            {
                return;
            }

            // citext keeps CLR strings unchanged while making equality and unique
            // indexes case-insensitive for identifiers in PostgreSQL.
            modelBuilder.Entity<User>(b =>
            {
                b.Property(x => x.UserName).HasColumnType("citext");
                b.Property(x => x.NormalizedUserName).HasColumnType("citext");
                b.Property(x => x.Email).HasColumnType("citext");
                b.Property(x => x.NormalizedEmail).HasColumnType("citext");
            });

            modelBuilder.Entity<Role>(b =>
            {
                b.Property(x => x.Key).HasColumnType("citext");
                b.Property(x => x.NormalizedName).HasColumnType("citext");
            });

            modelBuilder.Entity<Permission>(b => b.Property(x => x.Key).HasColumnType("citext"));
            modelBuilder.Entity<BusinessInvitation>(b => b.Property(x => x.NormalizedEmail).HasColumnType("citext"));
            modelBuilder.Entity<BillingPlan>(b => b.Property(x => x.Code).HasColumnType("citext"));

            modelBuilder.Entity<Brand>(b => b.Property(x => x.Slug).HasColumnType("citext"));
            modelBuilder.Entity<BrandTranslation>(b => b.Property(x => x.Culture).HasColumnType("citext"));
            modelBuilder.Entity<CategoryTranslation>(b =>
            {
                b.Property(x => x.Culture).HasColumnType("citext");
                b.Property(x => x.Slug).HasColumnType("citext");
            });
            modelBuilder.Entity<ProductTranslation>(b =>
            {
                b.Property(x => x.Culture).HasColumnType("citext");
                b.Property(x => x.Slug).HasColumnType("citext");
            });
            modelBuilder.Entity<ProductVariant>(b => b.Property(x => x.Sku).HasColumnType("citext"));
            modelBuilder.Entity<PageTranslation>(b =>
            {
                b.Property(x => x.Culture).HasColumnType("citext");
                b.Property(x => x.Slug).HasColumnType("citext");
            });

            modelBuilder.Entity<Promotion>(b => b.Property(x => x.Code).HasColumnType("citext"));
            modelBuilder.Entity<TaxCategory>(b => b.Property(x => x.Name).HasColumnType("citext"));
        }

        private void ApplyPostgreSqlJsonbColumns(ModelBuilder modelBuilder)
        {
            if (Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) != true)
            {
                return;
            }

            modelBuilder.Entity<Campaign>(b =>
            {
                b.Property(x => x.TargetingJson).HasColumnType("jsonb");
                b.Property(x => x.PayloadJson).HasColumnType("jsonb");
            });

            modelBuilder.Entity<BusinessSubscription>(b =>
                b.Property(x => x.MetadataJson).HasColumnType("jsonb"));

            modelBuilder.Entity<BusinessLocation>(b =>
                b.Property(x => x.OpeningHoursJson).HasColumnType("jsonb"));

            modelBuilder.Entity<SubscriptionInvoice>(b =>
            {
                b.Property(x => x.LinesJson).HasColumnType("jsonb");
                b.Property(x => x.MetadataJson).HasColumnType("jsonb");
            });

            modelBuilder.Entity<AnalyticsExportJob>(b =>
                b.Property(x => x.ParametersJson).HasColumnType("jsonb"));

            modelBuilder.Entity<UserEngagementSnapshot>(b =>
                b.Property(x => x.SnapshotJson).HasColumnType("jsonb"));

            modelBuilder.Entity<Order>(b =>
            {
                b.Property(x => x.BillingAddressJson).HasColumnType("jsonb");
                b.Property(x => x.ShippingAddressJson).HasColumnType("jsonb");
            });

            modelBuilder.Entity<OrderLine>(b =>
                b.Property(x => x.AddOnValueIdsJson).HasColumnType("jsonb"));

            modelBuilder.Entity<Promotion>(b =>
                b.Property(x => x.ConditionsJson).HasColumnType("jsonb"));

            modelBuilder.Entity<SiteSetting>(b =>
            {
                b.Property(x => x.FeatureFlagsJson).HasColumnType("jsonb");
                b.Property(x => x.MeasurementSettingsJson).HasColumnType("jsonb");
                b.Property(x => x.NumberFormattingOverridesJson).HasColumnType("jsonb");
                b.Property(x => x.OpenGraphDefaultsJson).HasColumnType("jsonb");
                b.Property(x => x.SmsExtraSettingsJson).HasColumnType("jsonb");
            });

            modelBuilder.Entity<User>(b =>
            {
                b.Property(x => x.ChannelsOptInJson).HasColumnType("jsonb");
                b.Property(x => x.FirstTouchUtmJson).HasColumnType("jsonb");
                b.Property(x => x.LastTouchUtmJson).HasColumnType("jsonb");
                b.Property(x => x.ExternalIdsJson).HasColumnType("jsonb");
            });
        }
    }
}
