using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Queries;
using Darwin.Contracts.Common;
using Darwin.Domain.Entities.Marketing;
using FluentAssertions;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Unit tests for campaign parsing helpers used by <see cref="GetMyPromotionsHandler"/>.
/// These tests exercise private static helpers via reflection because they directly shape
/// mobile-facing promotion payloads and diagnostics while remaining internal implementation details.
/// </summary>
public sealed class GetMyPromotionsCampaignParsingTests
{
    private static readonly MethodInfo ResolveCampaignStateMethod =
        typeof(GetMyPromotionsHandler).GetMethod("ResolveCampaignState", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("GetMyPromotionsHandler.ResolveCampaignState was not found.");

    private static readonly MethodInfo ResolveCampaignPriorityMethod =
        typeof(GetMyPromotionsHandler).GetMethod("ResolveCampaignPriority", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("GetMyPromotionsHandler.ResolveCampaignPriority was not found.");

    private static readonly MethodInfo BuildEligibilityRulesMethod =
        typeof(GetMyPromotionsHandler).GetMethod("BuildEligibilityRulesFromTargeting", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("GetMyPromotionsHandler.BuildEligibilityRulesFromTargeting was not found.");

    /// <summary>
    /// Verifies lifecycle-state resolution for the four operator-visible states used by the
    /// promotions feed so schedule regressions are caught before mobile clients consume them.
    /// </summary>
    [Theory]
    [MemberData(nameof(ResolveCampaignStateCases))]
    public void ResolveCampaignState_Should_ReturnExpectedLifecycleState(Campaign campaign, DateTime nowUtc, string expectedState)
    {
        // Act
        var state = InvokeResolveCampaignState(campaign, nowUtc);

        // Assert
        state.Should().Be(expectedState);
    }

    /// <summary>
    /// Verifies that priority extraction preserves explicit values, clamps extremes, and falls
    /// back to the stable default when payload JSON is absent or malformed.
    /// </summary>
    [Theory]
    [InlineData(null, 120)]
    [InlineData("", 120)]
    [InlineData("{\"priority\":42}", 42)]
    [InlineData("{\"priority\":\"73\"}", 73)]
    [InlineData("{\"priority\":5000}", 1000)]
    [InlineData("{\"priority\":-8}", 0)]
    [InlineData("{not-json}", 120)]
    public void ResolveCampaignPriority_Should_NormalizePayloadValues(string? payloadJson, int expectedPriority)
    {
        // Act
        var priority = InvokeResolveCampaignPriority(payloadJson);

        // Assert
        priority.Should().Be(expectedPriority);
    }

    /// <summary>
    /// Verifies that targeting payloads using the normalized <c>eligibilityRules</c> array are
    /// parsed into mobile-facing DTOs without losing tier/points/note metadata.
    /// </summary>
    [Fact]
    public void BuildEligibilityRulesFromTargeting_Should_ParseEligibilityRulesArray()
    {
        // Arrange
        const string targetingJson = """
        {
          "eligibilityRules": [
            {
              "audienceKind": "TierSegment",
              "tierKey": "gold",
              "note": "Gold members only"
            },
            {
              "audienceKind": "PointsThreshold",
              "minPoints": 120,
              "maxPoints": 199,
              "note": "Mid-tier balance window"
            }
          ]
        }
        """;

        // Act
        var rules = InvokeBuildEligibilityRules(targetingJson);

        // Assert
        rules.Should().HaveCount(2);
        rules[0].AudienceKind.Should().Be("TierSegment");
        rules[0].TierKey.Should().Be("gold");
        rules[0].Note.Should().Be("Gold members only");
        rules[1].AudienceKind.Should().Be("PointsThreshold");
        rules[1].MinPoints.Should().Be(120);
        rules[1].MaxPoints.Should().Be(199);
        rules[1].Note.Should().Be("Mid-tier balance window");
    }

    /// <summary>
    /// Verifies that legacy compact targeting fields still map to normalized eligibility DTOs,
    /// preserving backward compatibility with older campaign payloads.
    /// </summary>
    [Fact]
    public void BuildEligibilityRulesFromTargeting_Should_ParseLegacyCompactFields()
    {
        // Arrange
        const string targetingJson = """
        {
          "minPoints": 250,
          "tier": "platinum"
        }
        """;

        // Act
        var rules = InvokeBuildEligibilityRules(targetingJson);

        // Assert
        rules.Should().HaveCount(2);
        rules.Should().ContainSingle(x => x.AudienceKind == "PointsThreshold" && x.MinPoints == 250);
        rules.Should().ContainSingle(x => x.AudienceKind == "TierSegment" && x.TierKey == "platinum");
    }

    /// <summary>
    /// Verifies that empty or malformed targeting JSON falls back to the joined-members default
    /// instead of failing the promotions feed.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{not-json}")]
    public void BuildEligibilityRulesFromTargeting_Should_FallbackToJoinedMembers_ForEmptyOrMalformedPayload(string? targetingJson)
    {
        // Act
        var rules = InvokeBuildEligibilityRules(targetingJson);

        // Assert
        rules.Should().ContainSingle();
        rules[0].AudienceKind.Should().Be("JoinedMembers");
        rules[0].Note.Should().Be("Campaign is visible to joined members.");
    }

    /// <summary>
    /// Supplies the lifecycle-state scenarios used by the promotions feed contract.
    /// </summary>
    public static IEnumerable<object[]> ResolveCampaignStateCases()
    {
        var nowUtc = new DateTime(2026, 03, 20, 12, 00, 00, DateTimeKind.Utc);

        yield return
        [
            new Campaign
            {
                IsActive = false,
                StartsAtUtc = nowUtc.AddHours(-1),
                EndsAtUtc = nowUtc.AddHours(2)
            },
            nowUtc,
            "Draft"
        ];

        yield return
        [
            new Campaign
            {
                IsActive = true,
                StartsAtUtc = nowUtc.AddHours(3),
                EndsAtUtc = nowUtc.AddDays(1)
            },
            nowUtc,
            "Scheduled"
        ];

        yield return
        [
            new Campaign
            {
                IsActive = true,
                StartsAtUtc = nowUtc.AddDays(-2),
                EndsAtUtc = nowUtc.AddDays(-1)
            },
            nowUtc,
            "Expired"
        ];

        yield return
        [
            new Campaign
            {
                IsActive = true,
                StartsAtUtc = nowUtc.AddHours(-4),
                EndsAtUtc = nowUtc.AddHours(4)
            },
            nowUtc,
            "Active"
        ];
    }

    private static string InvokeResolveCampaignState(Campaign campaign, DateTime nowUtc)
    {
        var raw = ResolveCampaignStateMethod.Invoke(null, [campaign, nowUtc]);
        raw.Should().BeOfType<string>();
        return (string)raw!;
    }

    private static int InvokeResolveCampaignPriority(string? payloadJson)
    {
        var raw = ResolveCampaignPriorityMethod.Invoke(null, [payloadJson]);
        raw.Should().BeOfType<int>();
        return (int)raw!;
    }

    private static List<PromotionEligibilityRuleDto> InvokeBuildEligibilityRules(string? targetingJson)
    {
        var raw = BuildEligibilityRulesMethod.Invoke(null, [targetingJson]);
        raw.Should().NotBeNull();
        return raw as List<PromotionEligibilityRuleDto>
               ?? throw new InvalidOperationException("BuildEligibilityRulesFromTargeting did not return the expected rule list.");
    }

    /// <summary>
    /// Verifies that the promotions diagnostics contract keeps a stable JSON shape for the
    /// operational counters consumed by mobile troubleshooting workflows:
    /// <c>initialCandidates</c>, <c>suppressedByFrequency</c>, <c>deduplicated</c>,
    /// <c>trimmedByCap</c>, and <c>finalCount</c>.
    ///
    /// The test discovers the diagnostics DTO via reflection to avoid coupling this unit-test file
    /// to a specific type name while still enforcing the required contract fields and round-trip behavior.
    /// </summary>
    [Fact]
    public void PromotionsDiagnosticsContract_Should_SerializeAndDeserialize_WithStableCounterPropertyNames()
    {
        // Arrange
        var diagnosticsType = FindPromotionsDiagnosticsContractType();
        var diagnostics = Activator.CreateInstance(diagnosticsType)
            ?? throw new InvalidOperationException("Could not create diagnostics DTO instance.");

        SetRequiredInt32Property(diagnosticsType, diagnostics, "InitialCandidates", 11);
        SetRequiredInt32Property(diagnosticsType, diagnostics, "SuppressedByFrequency", 4);
        SetRequiredInt32Property(diagnosticsType, diagnostics, "Deduplicated", 2);
        SetRequiredInt32Property(diagnosticsType, diagnostics, "TrimmedByCap", 1);
        SetRequiredInt32Property(diagnosticsType, diagnostics, "FinalCount", 4);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act
        var json = JsonSerializer.Serialize(diagnostics, diagnosticsType, jsonOptions);
        var roundTripped = JsonSerializer.Deserialize(json, diagnosticsType, jsonOptions);

        // Assert
        json.Should().Contain("\"initialCandidates\":11");
        json.Should().Contain("\"suppressedByFrequency\":4");
        json.Should().Contain("\"deduplicated\":2");
        json.Should().Contain("\"trimmedByCap\":1");
        json.Should().Contain("\"finalCount\":4");

        roundTripped.Should().NotBeNull();
        GetRequiredInt32Property(diagnosticsType, roundTripped!, "InitialCandidates").Should().Be(11);
        GetRequiredInt32Property(diagnosticsType, roundTripped!, "SuppressedByFrequency").Should().Be(4);
        GetRequiredInt32Property(diagnosticsType, roundTripped!, "Deduplicated").Should().Be(2);
        GetRequiredInt32Property(diagnosticsType, roundTripped!, "TrimmedByCap").Should().Be(1);
        GetRequiredInt32Property(diagnosticsType, roundTripped!, "FinalCount").Should().Be(4);
    }

    /// <summary>
    /// Locates the promotions diagnostics DTO in <c>Darwin.Contracts.Loyalty</c> by required
    /// integer counter properties instead of relying on a hard-coded type name.
    /// </summary>
    private static Type FindPromotionsDiagnosticsContractType()
    {
        var contractsAssembly = typeof(ApiEnvelope<object>).Assembly;
        var requiredProperties = new[]
        {
            "InitialCandidates",
            "SuppressedByFrequency",
            "Deduplicated",
            "TrimmedByCap",
            "FinalCount"
        };

        var diagnosticsType = contractsAssembly
            .GetTypes()
            .Where(x => string.Equals(x.Namespace, "Darwin.Contracts.Loyalty", StringComparison.Ordinal))
            .FirstOrDefault(x =>
                requiredProperties.All(propertyName =>
                {
                    var property = x.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    return property?.PropertyType == typeof(int);
                }));

        diagnosticsType.Should().NotBeNull("a loyalty diagnostics DTO with required counter properties must exist in Darwin.Contracts.");
        return diagnosticsType!;
    }

    /// <summary>
    /// Sets a required integer property on a reflected DTO instance with explicit guardrails.
    /// </summary>
    private static void SetRequiredInt32Property(Type ownerType, object instance, string propertyName, int value)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        property.Should().NotBeNull($"property '{propertyName}' must exist on {ownerType.FullName}.");
        property!.PropertyType.Should().Be(typeof(int), $"property '{propertyName}' must be Int32 for stable counter transport.");
        property.SetValue(instance, value);
    }

    /// <summary>
    /// Reads a required integer property from a reflected DTO instance.
    /// </summary>
    private static int GetRequiredInt32Property(Type ownerType, object instance, string propertyName)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        property.Should().NotBeNull($"property '{propertyName}' must exist on {ownerType.FullName}.");
        property!.PropertyType.Should().Be(typeof(int), $"property '{propertyName}' must be Int32 for stable counter transport.");
        return (int)(property.GetValue(instance)
               ?? throw new InvalidOperationException($"Property '{propertyName}' value was null."));
    }
}
