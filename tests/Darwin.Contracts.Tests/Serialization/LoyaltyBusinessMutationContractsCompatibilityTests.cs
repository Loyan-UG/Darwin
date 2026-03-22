using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Darwin.Contracts.Common;
using FluentAssertions;

namespace Darwin.Contracts.Tests.Serialization;

/// <summary>
/// Provides contract-compatibility coverage for loyalty business mutation DTOs.
/// 
/// Why these tests exist:
/// - Mobile Business editor flows depend on stable JSON field names and payload shapes.
/// - Mutation requests commonly include optimistic-concurrency tokens (<c>RowVersion</c>),
///   which must serialize as base64 strings for HTTP transport.
/// - Contract drift in these DTOs can silently break mobile create/update/delete operations.
/// 
/// Strategy:
/// - Reflection-based discovery in <c>Darwin.Contracts.Loyalty</c> keeps tests resilient
///   to DTO type renames while still enforcing field-level compatibility guarantees.
/// </summary>
public sealed class LoyaltyBusinessMutationContractsCompatibilityTests
{
    /// <summary>
    /// Verifies that all loyalty contract DTOs exposing a public <c>RowVersion</c> byte-array property
    /// serialize that property as base64 JSON string and round-trip back to byte[].
    /// 
    /// This assertion protects update/delete mutation transport semantics used by optimistic concurrency.
    /// </summary>
    [Fact]
    public void LoyaltyContracts_WithRowVersionByteArray_Should_SerializeAsBase64_AndRoundTrip()
    {
        // Arrange
        var contractTypesWithRowVersion = FindLoyaltyContractTypesWithByteArrayProperty("RowVersion");
        contractTypesWithRowVersion.Should().NotBeEmpty(
            "at least one loyalty contract DTO should expose RowVersion for optimistic concurrency transport.");

        var rowVersionBytes = new byte[] { 1, 2, 3, 4, 5 };
        var expectedBase64 = Convert.ToBase64String(rowVersionBytes);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act + Assert
        foreach (var dtoType in contractTypesWithRowVersion)
        {
            var instance = Activator.CreateInstance(dtoType)
                ?? throw new InvalidOperationException($"Could not create instance of {dtoType.FullName}.");

            SetRequiredProperty(dtoType, instance, "RowVersion", rowVersionBytes);

            var json = JsonSerializer.Serialize(instance, dtoType, options);
            json.Should().Contain($"\"rowVersion\":\"{expectedBase64}\"",
                $"DTO '{dtoType.Name}' must serialize RowVersion as base64 string.");

            var roundTripped = JsonSerializer.Deserialize(json, dtoType, options);
            roundTripped.Should().NotBeNull();

            var returned = GetRequiredProperty<byte[]>(dtoType, roundTripped!, "RowVersion");
            returned.Should().Equal(rowVersionBytes,
                $"DTO '{dtoType.Name}' must preserve RowVersion bytes across JSON round-trip.");
        }
    }

    /// <summary>
    /// Verifies that loyalty business mutation contracts still expose stable core fields
    /// for campaign and reward-tier editor flows.
    /// 
    /// Required groups:
    /// - Campaign mutation-like DTO: Name/Title/Channels/TargetingJson/PayloadJson
    /// - Reward-tier mutation-like DTO: PointsRequired/RewardType/AllowSelfRedemption
    /// 
    /// The test asserts both CLR property presence and JSON camelCase output names.
    /// </summary>
    [Fact]
    public void LoyaltyBusinessMutationContracts_Should_ExposeStableCoreFields_ForCampaignAndRewardEditors()
    {
        // Arrange
        var contractsAssembly = typeof(ApiEnvelope<object>).Assembly;
        var loyaltyTypes = contractsAssembly
            .GetTypes()
            .Where(t => string.Equals(t.Namespace, "Darwin.Contracts.Loyalty", StringComparison.Ordinal))
            .ToList();

        loyaltyTypes.Should().NotBeEmpty("Darwin.Contracts.Loyalty namespace should contain mutation DTOs.");

        var campaignCoreFields = new[] { "Name", "Title", "Channels", "TargetingJson", "PayloadJson" };
        var rewardCoreFields = new[] { "PointsRequired", "RewardType", "AllowSelfRedemption" };

        var campaignType = FindTypeContainingProperties(loyaltyTypes, campaignCoreFields);
        var rewardType = FindTypeContainingProperties(loyaltyTypes, rewardCoreFields);

        campaignType.Should().NotBeNull("a campaign mutation-like DTO with required editor fields must exist.");
        rewardType.Should().NotBeNull("a reward-tier mutation-like DTO with required editor fields must exist.");

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act
        var campaignJson = SerializeWithRepresentativeValues(campaignType!, options, new Dictionary<string, object?>
        {
            ["Name"] = "spring-boost",
            ["Title"] = "Spring Boost",
            ["Channels"] = (short)3,
            ["TargetingJson"] = "{\"audienceKind\":\"JoinedMembers\"}",
            ["PayloadJson"] = "{\"cta\":\"OpenApp\"}"
        });

        var rewardJson = SerializeWithRepresentativeValues(rewardType!, options, new Dictionary<string, object?>
        {
            ["PointsRequired"] = 120,
            ["RewardType"] = "AmountDiscount",
            ["AllowSelfRedemption"] = true
        });

        // Assert
        campaignJson.Should().Contain("\"name\":\"spring-boost\"");
        campaignJson.Should().Contain("\"title\":\"Spring Boost\"");
        campaignJson.Should().Contain("\"channels\":3");
        campaignJson.Should().Contain("\"targetingJson\":\"{\\u0022audienceKind\\u0022:\\u0022JoinedMembers\\u0022}\"");
        campaignJson.Should().Contain("\"payloadJson\":\"{\\u0022cta\\u0022:\\u0022OpenApp\\u0022}\"");

        rewardJson.Should().Contain("\"pointsRequired\":120");
        rewardJson.Should().Contain("\"rewardType\":\"AmountDiscount\"");
        rewardJson.Should().Contain("\"allowSelfRedemption\":true");
    }

    /// <summary>
    /// Finds all loyalty contract types that contain a public instance property with the specified name
    /// and exact <see cref="byte[]"/> type.
    /// </summary>
    private static IReadOnlyList<Type> FindLoyaltyContractTypesWithByteArrayProperty(string propertyName)
    {
        var contractsAssembly = typeof(ApiEnvelope<object>).Assembly;

        return contractsAssembly
            .GetTypes()
            .Where(t => string.Equals(t.Namespace, "Darwin.Contracts.Loyalty", StringComparison.Ordinal))
            .Where(t =>
            {
                var property = t.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                return property?.PropertyType == typeof(byte[]);
            })
            .ToList();
    }

    /// <summary>
    /// Finds first type containing all specified public instance properties (case-insensitive).
    /// </summary>
    private static Type? FindTypeContainingProperties(IEnumerable<Type> candidates, IReadOnlyCollection<string> propertyNames)
    {
        return candidates.FirstOrDefault(candidate =>
            propertyNames.All(name =>
                candidate.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) is not null));
    }

    /// <summary>
    /// Creates an instance of the specified DTO type, assigns representative values, and serializes it with web defaults.
    /// </summary>
    private static string SerializeWithRepresentativeValues(
        Type dtoType,
        JsonSerializerOptions options,
        IReadOnlyDictionary<string, object?> valuesByProperty)
    {
        var instance = Activator.CreateInstance(dtoType)
            ?? throw new InvalidOperationException($"Could not create instance of {dtoType.FullName}.");

        foreach (var pair in valuesByProperty)
        {
            if (HasWritableProperty(dtoType, pair.Key))
            {
                SetRequiredProperty(dtoType, instance, pair.Key, pair.Value);
            }
        }

        return JsonSerializer.Serialize(instance, dtoType, options);
    }

    /// <summary>
    /// Returns true when the property exists and can be assigned (set/init).
    /// </summary>
    private static bool HasWritableProperty(Type ownerType, string propertyName)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return property is not null;
    }

    /// <summary>
    /// Sets a required property value via reflection.
    /// </summary>
    private static void SetRequiredProperty(Type ownerType, object instance, string propertyName, object? value)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        property.Should().NotBeNull($"property '{propertyName}' must exist on {ownerType.FullName}.");
        property!.SetValue(instance, value);
    }

    /// <summary>
    /// Reads and casts a required property value via reflection.
    /// </summary>
    private static T GetRequiredProperty<T>(Type ownerType, object instance, string propertyName)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        property.Should().NotBeNull($"property '{propertyName}' must exist on {ownerType.FullName}.");

        var raw = property!.GetValue(instance);
        raw.Should().BeAssignableTo<T>($"property '{propertyName}' must be assignable to {typeof(T).Name}.");
        return (T)raw!;
    }
}