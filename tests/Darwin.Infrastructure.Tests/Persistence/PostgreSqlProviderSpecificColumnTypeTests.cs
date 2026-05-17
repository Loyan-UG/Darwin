using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
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
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class PostgreSqlProviderSpecificColumnTypeTests
{
    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Fact]
    public void NpgsqlModel_Should_UseCitextForConfiguredIdentifierColumns()
    {
        using var context = new DarwinDbContext(
            new DbContextOptionsBuilder<DarwinDbContext>()
                .UseNpgsql(DummyPostgreSqlConnectionString)
                .Options);

        var expected = new Dictionary<Type, string[]>
        {
            [typeof(User)] = ["UserName", "NormalizedUserName", "Email", "NormalizedEmail"],
            [typeof(Role)] = ["Key", "NormalizedName"],
            [typeof(Permission)] = ["Key"],
            [typeof(BusinessInvitation)] = ["NormalizedEmail"],
            [typeof(BillingPlan)] = ["Code"],
            [typeof(Brand)] = ["Slug"],
            [typeof(BrandTranslation)] = ["Culture"],
            [typeof(CategoryTranslation)] = ["Culture", "Slug"],
            [typeof(ProductTranslation)] = ["Culture", "Slug"],
            [typeof(ProductVariant)] = ["Sku"],
            [typeof(PageTranslation)] = ["Culture", "Slug"],
            [typeof(Promotion)] = ["Code"],
            [typeof(TaxCategory)] = ["Name"],
            [typeof(RedirectRule)] = ["FromPath", "To"]
        };

        var failures = expected.SelectMany(entry =>
            GetProperties(context, entry.Key)
                .Where(property => entry.Value.Contains(property.Name))
                .Select(property => new
                {
                    Entity = entry.Key.Name,
                    Property = property.Name,
                    ActualType = property.GetColumnType()
                })
                .Where(x => !string.Equals(x.ActualType, "citext", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        failures.Should().BeEmpty("all configured PostgreSQL identifier properties should be mapped as citext");
    }

    [Fact]
    public void NpgsqlModel_Should_UseJsonbForConfiguredJsonColumns()
    {
        using var context = new DarwinDbContext(
            new DbContextOptionsBuilder<DarwinDbContext>()
                .UseNpgsql(DummyPostgreSqlConnectionString)
                .Options);

        var expected = new Dictionary<Type, string[]>
        {
            [typeof(Campaign)] = ["TargetingJson", "PayloadJson"],
            [typeof(BusinessSubscription)] = ["MetadataJson"],
            [typeof(BusinessLocation)] = ["OpeningHoursJson"],
            [typeof(SubscriptionInvoice)] = ["LinesJson", "MetadataJson"],
            [typeof(AnalyticsExportJob)] = ["ParametersJson"],
            [typeof(UserEngagementSnapshot)] = ["SnapshotJson"],
            [typeof(Order)] = ["BillingAddressJson", "ShippingAddressJson"],
            [typeof(OrderLine)] = ["AddOnValueIdsJson"],
            [typeof(Promotion)] = ["ConditionsJson"],
            [typeof(SiteSetting)] = ["FeatureFlagsJson", "MeasurementSettingsJson", "NumberFormattingOverridesJson", "OpenGraphDefaultsJson", "SmsExtraSettingsJson"],
            [typeof(User)] = ["ChannelsOptInJson", "FirstTouchUtmJson", "LastTouchUtmJson", "ExternalIdsJson"],
        };

        var failures = expected.SelectMany(entry =>
            GetProperties(context, entry.Key)
                .Where(property => entry.Value.Contains(property.Name))
                .Select(property => new
                {
                    Entity = entry.Key.Name,
                    Property = property.Name,
                    ActualType = property.GetColumnType()
                })
                .Where(x => !string.Equals(x.ActualType, "jsonb", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        failures.Should().BeEmpty("all configured PostgreSQL JSON columns should be mapped as jsonb");
    }

    private static IEnumerable<IProperty> GetProperties(DarwinDbContext context, Type entityType)
    {
        var entity = context.Model.GetEntityTypes().FirstOrDefault(type => type.ClrType == entityType);
        entity.Should().NotBeNull($"entity {entityType.Name} should be configured");

        return entity!.GetProperties();
    }
}
