using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class PostgreSqlSearchIndexCoverageTests
{
    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Fact]
    public void GenerateCreateScript_ForPostgreSql_Should_ContainExpectedSearchExtensionsAndIndexes()
    {
        using var context = new DarwinDbContext(
            new DbContextOptionsBuilder<DarwinDbContext>()
                .UseNpgsql(DummyPostgreSqlConnectionString)
                .Options);

        var script = context.Database.GenerateCreateScript();

        script.Should().Contain("""CREATE EXTENSION IF NOT EXISTS pg_trgm;""");
        script.Should().Contain("""CREATE EXTENSION IF NOT EXISTS citext;""");

        var likeTrgmMatches = Regex.Matches(script, "\"IX_PG_[^\"]+?_Like_Trgm\"");
        var jsonbGinMatches = Regex.Matches(script, "\"IX_PG_[^\"]+?_JsonbGin\"");

        likeTrgmMatches.Count.Should().BeGreaterOrEqualTo(88, "operational and search surface should include all required direct LIKE trigram indexes");
        jsonbGinMatches.Count.Should().Be(14, "JSON/search-backed columns should expose exactly 14 JSONB GIN indexes in the configured provider migrations");

        var expectedJsonbIndexes = new[]
        {
            "IX_PG_Users_LastTouchUtmJson_JsonbGin",
            "IX_PG_Users_FirstTouchUtmJson_JsonbGin",
            "IX_PG_Users_ExternalIdsJson_JsonbGin",
            "IX_PG_Users_ChannelsOptInJson_JsonbGin",
            "IX_PG_SubscriptionInvoices_MetadataJson_JsonbGin",
            "IX_PG_SubscriptionInvoices_LinesJson_JsonbGin",
            "IX_PG_Promotions_ConditionsJson_JsonbGin",
            "IX_PG_Campaigns_TargetingJson_JsonbGin",
            "IX_PG_Campaigns_PayloadJson_JsonbGin",
            "IX_PG_BusinessSubscriptions_MetadataJson_JsonbGin",
            "IX_PG_UserEngagementSnapshots_SnapshotJson_JsonbGin",
            "IX_PG_SiteSettings_FeatureFlagsJson_JsonbGin",
            "IX_PG_BusinessLocations_OpeningHoursJson_JsonbGin",
            "IX_PG_AnalyticsExportJobs_ParametersJson_JsonbGin",
        };

        foreach (var indexName in expectedJsonbIndexes)
        {
            script.Should().Contain($@"""{indexName}""");
        }

        script.Should().Contain(@"""IX_PG_ProductTranslations_Name_Like_Trgm""");
        script.Should().Contain(@"""IX_PG_EventLogs_PropertiesJson_Trgm""");
        script.Should().Contain(@"""IX_PG_Payments_FailureReason_Like_Trgm""");
    }

    [Fact]
    public void GenerateCreateScript_ForPostgreSql_Should_ContainTextBackedJsonTrgmIndexes()
    {
        using var context = new DarwinDbContext(
            new DbContextOptionsBuilder<DarwinDbContext>()
                .UseNpgsql(DummyPostgreSqlConnectionString)
                .Options);

        var script = context.Database.GenerateCreateScript();

        var expectedTextJsonTrgmIndexes = new[]
        {
            "IX_PG_EventLogs_PropertiesJson_Trgm",
            "IX_PG_ProviderCallbackInboxMessages_PayloadJson_Trgm",
            "IX_PG_Businesses_AdminTextOverridesJson_Trgm"
        };

        foreach (var indexName in expectedTextJsonTrgmIndexes)
        {
            script.Should().Contain($@"""{indexName}""");
        }
    }

    [Fact]
    public void GenerateCreateScript_ForPostgreSql_Should_ContainJsonValidityConstraints()
    {
        using var context = new DarwinDbContext(
            new DbContextOptionsBuilder<DarwinDbContext>()
                .UseNpgsql(DummyPostgreSqlConnectionString)
                .Options);

        var script = context.Database.GenerateCreateScript();

        script.Should().Contain("""CREATE OR REPLACE FUNCTION public.darwin_is_valid_jsonb(value text)""");

        var expectedJsonValidityConstraints = new[]
        {
            "CK_PG_BillingPlans_FeaturesJson_ValidJson",
            "CK_PG_Businesses_AdminTextOverridesJson_ValidJson",
            "CK_PG_Carts_SelectedAddOnValueIdsJson_ValidJson",
            "CK_PG_EventLogs_PropertiesJson_ValidJson",
            "CK_PG_EventLogs_UtmSnapshotJson_ValidJson",
            "CK_PG_ProviderCallbackInboxMessages_PayloadJson_ValidJson",
            "CK_PG_LoyaltyPrograms_RulesJson_ValidJson",
            "CK_PG_LoyaltyRewardRedemptions_MetadataJson_ValidJson",
            "CK_PG_LoyaltyRewardTiers_MetadataJson_ValidJson",
            "CK_PG_ScanSessions_SelectedRewardsJson_ValidJson",
            "CK_PG_SiteSettings_AdminTextOverridesJson_ValidJson"
        };

        foreach (var constraintName in expectedJsonValidityConstraints)
        {
            script.Should().Contain($@"""{constraintName}""");
        }

        foreach (var column in new[] { "FeaturesJson", "AdminTextOverridesJson", "PropertiesJson", "UtmSnapshotJson", "PayloadJson", "RulesJson", "MetadataJson", "SelectedAddOnValueIdsJson", "SelectedRewardsJson" })
        {
            script.Should().Contain($@"public.darwin_is_valid_jsonb(\""{column}\"")");
        }
    }

    [Fact]
    public void GenerateCreateScript_ForPostgreSql_Should_PreferCitextColumnsForIdentifierSemantics()
    {
        using var context = new DarwinDbContext(
            new DbContextOptionsBuilder<DarwinDbContext>()
                .UseNpgsql(DummyPostgreSqlConnectionString)
                .Options);

        var script = context.Database.GenerateCreateScript();
        var identifierColumns = new[]
        {
            "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
            "Key", "NormalizedName", "Code", "Slug", "Culture", "Sku", "Name", "FromPath", "To"
        };

        script.Should().Contain("""CREATE EXTENSION IF NOT EXISTS citext;""");

        foreach (var column in identifierColumns)
        {
            script.Should().NotContain($"LOWER(\"{column}\")");
            script.Should().NotContain($"UPPER(\"{column}\")");
        }

        foreach (var column in identifierColumns)
        {
            script.Should().Contain($@"""{column}""");
        }
    }
}
