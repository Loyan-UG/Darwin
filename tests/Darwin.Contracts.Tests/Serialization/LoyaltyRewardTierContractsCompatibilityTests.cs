using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Darwin.Contracts.Common;
using FluentAssertions;

namespace Darwin.Contracts.Tests.Serialization;

/// <summary>
/// Contract-compatibility coverage for loyalty reward-tier DTOs used by business editor flows.
/// 
/// Goals:
/// - Keep reward-tier core field names stable for mobile UI bindings.
/// - Ensure optimistic-concurrency row-version transport remains base64-compatible.
/// - Avoid brittle coupling to exact DTO class names by using reflection-based discovery.
/// </summary>
public sealed class LoyaltyRewardTierContractsCompatibilityTests
{
    /// <summary>
    /// Verifies that reward-tier editor core fields serialize with stable camelCase names:
    /// <c>pointsRequired</c>, <c>rewardType</c>, <c>allowSelfRedemption</c>.
    /// </summary>
    [Fact]
    public void LoyaltyRewardTierLikeContracts_Should_SerializeStableCoreEditorFields()
    {
        // Arrange
        var loyaltyTypes = GetLoyaltyContractTypes();

        var rewardLikeTypes = loyaltyTypes
            .Where(t =>
                HasProperty(t, "PointsRequired") &&
                HasProperty(t, "RewardType") &&
                HasProperty(t, "AllowSelfRedemption"))
            .ToList();

        rewardLikeTypes.Should().NotBeEmpty(
            "at least one reward-tier-like DTO with editor core fields must exist.");

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act + Assert
        foreach (var dtoType in rewardLikeTypes)
        {
            var instance = Activator.CreateInstance(dtoType)
                ?? throw new InvalidOperationException($"Could not create instance for {dtoType.FullName}.");

            SetIfPresent(dtoType, instance, "PointsRequired", ConvertToPropertyType(dtoType, "PointsRequired", 150));
            SetIfPresent(dtoType, instance, "RewardType", "AmountDiscount");
            SetIfPresent(dtoType, instance, "AllowSelfRedemption", true);

            if (HasProperty(dtoType, "Description"))
            {
                SetIfPresent(dtoType, instance, "Description", "Spring campaign reward tier");
            }

            if (HasProperty(dtoType, "RewardValue"))
            {
                SetIfPresent(dtoType, instance, "RewardValue", ConvertToPropertyType(dtoType, "RewardValue", 12.5m));
            }

            var json = JsonSerializer.Serialize(instance, dtoType, options);

            json.Should().Contain("\"pointsRequired\":150");
            json.Should().Contain("\"rewardType\":\"AmountDiscount\"");
            json.Should().Contain("\"allowSelfRedemption\":true");
        }
    }

    /// <summary>
    /// Verifies that reward-tier update/delete-like DTOs carrying both
    /// <c>RewardTierId</c> and <c>RowVersion</c> keep stable GUID/base64 round-trip transport.
    /// </summary>
    [Fact]
    public void LoyaltyRewardTierMutationLikeContracts_Should_RoundTripRewardTierIdAndRowVersion()
    {
        // Arrange
        var loyaltyTypes = GetLoyaltyContractTypes();

        var mutationLikeTypes = loyaltyTypes
            .Where(t =>
                HasProperty(t, "RewardTierId") &&
                HasProperty(t, "RowVersion") &&
                IsByteArrayProperty(t, "RowVersion"))
            .ToList();

        mutationLikeTypes.Should().NotBeEmpty(
            "at least one reward-tier mutation-like DTO with RewardTierId + RowVersion must exist.");

        var expectedId = Guid.Parse("d8f264d4-0a9e-4d85-a73f-c5f5e4c7f3c1");
        var rowVersionBytes = new byte[] { 1, 3, 5, 7, 9 };
        var expectedBase64 = Convert.ToBase64String(rowVersionBytes);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act + Assert
        foreach (var dtoType in mutationLikeTypes)
        {
            var instance = Activator.CreateInstance(dtoType)
                ?? throw new InvalidOperationException($"Could not create instance for {dtoType.FullName}.");

            SetIfPresent(dtoType, instance, "RewardTierId", ConvertToPropertyType(dtoType, "RewardTierId", expectedId));
            SetIfPresent(dtoType, instance, "RowVersion", rowVersionBytes);

            var json = JsonSerializer.Serialize(instance, dtoType, options);
            var roundTrip = JsonSerializer.Deserialize(json, dtoType, options);

            json.Should().Contain("\"rewardTierId\":\"d8f264d4-0a9e-4d85-a73f-c5f5e4c7f3c1\"");
            json.Should().Contain($"\"rowVersion\":\"{expectedBase64}\"");

            roundTrip.Should().NotBeNull();
            GetRequiredProperty<Guid>(dtoType, roundTrip!, "RewardTierId").Should().Be(expectedId);
            GetRequiredProperty<byte[]>(dtoType, roundTrip!, "RowVersion").Should().Equal(rowVersionBytes);
        }
    }

    /// <summary>
    /// Verifies that reward-tier-like contracts exposing an optional <c>RewardValue</c> field
    /// preserve both explicit numeric values and explicit null values across JSON round-trip.
    ///
    /// Why this matters:
    /// - Mobile editor must support reward types where value is optional.
    /// - Contract drift around nullable numeric transport can break create/update payload semantics.
    /// </summary>
    [Fact]
    public void LoyaltyRewardTierLikeContracts_WithRewardValue_Should_RoundTripNumericAndNullValues()
    {
        // Arrange
        var loyaltyTypes = GetLoyaltyContractTypes();

        var valueAwareTypes = loyaltyTypes
            .Where(t =>
                HasProperty(t, "RewardType") &&
                HasProperty(t, "RewardValue"))
            .ToList();

        valueAwareTypes.Should().NotBeEmpty(
            "at least one reward-tier-like DTO with RewardType + RewardValue must exist.");

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act + Assert
        foreach (var dtoType in valueAwareTypes)
        {
            // --- Case A: numeric value ---
            var numericInstance = Activator.CreateInstance(dtoType)
                ?? throw new InvalidOperationException($"Could not create instance for {dtoType.FullName}.");

            SetIfPresent(dtoType, numericInstance, "RewardType", "PercentDiscount");
            SetIfPresent(dtoType, numericInstance, "RewardValue", ConvertToPropertyType(dtoType, "RewardValue", 15.5m));

            var numericJson = JsonSerializer.Serialize(numericInstance, dtoType, options);
            var numericRoundTrip = JsonSerializer.Deserialize(numericJson, dtoType, options);

            numericJson.Should().Contain("\"rewardType\":\"PercentDiscount\"");
            numericJson.Should().Contain("\"rewardValue\":");
            numericRoundTrip.Should().NotBeNull();
            GetRawPropertyValue(dtoType, numericRoundTrip!, "RewardValue")
                .Should().NotBeNull("numeric reward value must survive JSON round-trip.");

            // --- Case B: explicit null ---
            var nullInstance = Activator.CreateInstance(dtoType)
                ?? throw new InvalidOperationException($"Could not create instance for {dtoType.FullName}.");

            SetIfPresent(dtoType, nullInstance, "RewardType", "FreeItem");
            SetIfPresent(dtoType, nullInstance, "RewardValue", null);

            var nullJson = JsonSerializer.Serialize(nullInstance, dtoType, options);
            var nullRoundTrip = JsonSerializer.Deserialize(nullJson, dtoType, options);

            nullJson.Should().Contain("\"rewardType\":\"FreeItem\"");
            nullJson.Should().Contain("\"rewardValue\":null");
            nullRoundTrip.Should().NotBeNull();
            GetRawPropertyValue(dtoType, nullRoundTrip!, "RewardValue")
                .Should().BeNull("null reward value must remain null after JSON round-trip.");
        }
    }

    /// <summary>
    /// Verifies that loyalty mutation-result-like contracts exposing a boolean status field
    /// (<c>Succeeded</c>) and a textual status detail (<c>Error</c> or <c>Message</c>)
    /// keep stable camelCase transport and round-trip behavior.
    ///
    /// Why this matters:
    /// - Business editor UX depends on deterministic success/failure status payloads.
    /// - Contract-level drift in status fields can silently break error rendering logic.
    /// </summary>
    [Fact]
    public void LoyaltyMutationResultLikeContracts_Should_RoundTripSucceededAndStatusTextFields()
    {
        // Arrange
        var loyaltyTypes = GetLoyaltyContractTypes();

        var mutationResultLikeTypes = loyaltyTypes
            .Where(t => IsBooleanProperty(t, "Succeeded") &&
                        (IsStringLikeProperty(t, "Error") || IsStringLikeProperty(t, "Message")))
            .ToList();

        mutationResultLikeTypes.Should().NotBeEmpty(
            "at least one loyalty mutation-result-like DTO with Succeeded + Error/Message must exist.");

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act + Assert
        foreach (var dtoType in mutationResultLikeTypes)
        {
            var statusTextPropertyName = ResolveStatusTextPropertyName(dtoType);

            // --- Success payload ---
            var successInstance = Activator.CreateInstance(dtoType)
                ?? throw new InvalidOperationException($"Could not create instance for {dtoType.FullName}.");

            SetIfPresent(dtoType, successInstance, "Succeeded", true);
            SetIfPresent(dtoType, successInstance, statusTextPropertyName, null);

            var successJson = JsonSerializer.Serialize(successInstance, dtoType, options);
            var successRoundTrip = JsonSerializer.Deserialize(successJson, dtoType, options);

            successJson.Should().Contain("\"succeeded\":true");
            successRoundTrip.Should().NotBeNull();
            GetRequiredProperty<bool>(dtoType, successRoundTrip!, "Succeeded").Should().BeTrue();

            // --- Failure payload ---
            var failureInstance = Activator.CreateInstance(dtoType)
                ?? throw new InvalidOperationException($"Could not create instance for {dtoType.FullName}.");

            SetIfPresent(dtoType, failureInstance, "Succeeded", false);
            SetIfPresent(dtoType, failureInstance, statusTextPropertyName, "validation-failed");

            var failureJson = JsonSerializer.Serialize(failureInstance, dtoType, options);
            var failureRoundTrip = JsonSerializer.Deserialize(failureJson, dtoType, options);

            failureJson.Should().Contain("\"succeeded\":false");
            failureJson.Should().Contain($"\"{char.ToLowerInvariant(statusTextPropertyName[0])}{statusTextPropertyName[1..]}\":\"validation-failed\"");

            failureRoundTrip.Should().NotBeNull();
            GetRequiredProperty<bool>(dtoType, failureRoundTrip!, "Succeeded").Should().BeFalse();
            GetRawPropertyValue(dtoType, failureRoundTrip!, statusTextPropertyName)
                ?.ToString().Should().Be("validation-failed");
        }
    }

    private static IReadOnlyList<Type> GetLoyaltyContractTypes()
    {
        var contractsAssembly = typeof(ApiEnvelope<object>).Assembly;

        return contractsAssembly
            .GetTypes()
            .Where(t => string.Equals(t.Namespace, "Darwin.Contracts.Loyalty", StringComparison.Ordinal))
            .ToList();
    }

    private static bool HasProperty(Type ownerType, string propertyName) =>
        ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) is not null;

    private static bool IsByteArrayProperty(Type ownerType, string propertyName)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return property?.PropertyType == typeof(byte[]);
    }

    private static bool IsBooleanProperty(Type ownerType, string propertyName)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return property?.PropertyType == typeof(bool);
    }

    private static bool IsStringLikeProperty(Type ownerType, string propertyName)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property is null)
        {
            return false;
        }

        var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        return targetType == typeof(string);
    }

    private static void SetIfPresent(Type ownerType, object instance, string propertyName, object? value)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        property.Should().NotBeNull($"property '{propertyName}' must exist on {ownerType.FullName}.");
        property!.SetValue(instance, value);
    }

    private static T GetRequiredProperty<T>(Type ownerType, object instance, string propertyName)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        property.Should().NotBeNull($"property '{propertyName}' must exist on {ownerType.FullName}.");

        var raw = property!.GetValue(instance);
        raw.Should().BeAssignableTo<T>($"property '{propertyName}' must be assignable to {typeof(T).Name}.");
        return (T)raw!;
    }

    private static object? ConvertToPropertyType(Type ownerType, string propertyName, object value)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on {ownerType.FullName}.");

        var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        if (targetType == typeof(decimal) && value is double d)
        {
            return Convert.ToDecimal(d);
        }

        return Convert.ChangeType(value, targetType);
    }

    private static object? GetRawPropertyValue(Type ownerType, object instance, string propertyName)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        property.Should().NotBeNull($"property '{propertyName}' must exist on {ownerType.FullName}.");
        return property!.GetValue(instance);
    }

    private static string ResolveStatusTextPropertyName(Type ownerType)
    {
        if (HasProperty(ownerType, "Error"))
        {
            return "Error";
        }

        if (HasProperty(ownerType, "Message"))
        {
            return "Message";
        }

        throw new InvalidOperationException($"Type '{ownerType.FullName}' does not expose Error/Message status text field.");
    }
}