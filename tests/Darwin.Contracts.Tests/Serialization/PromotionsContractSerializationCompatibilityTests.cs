using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Darwin.Contracts.Common;
using FluentAssertions;

namespace Darwin.Contracts.Tests.Serialization;

/// <summary>
/// Contract-level serialization compatibility tests for promotions-related payloads.
/// 
/// Why reflection is used:
/// - This test suite enforces JSON shape stability without hard-coding internal DTO type names.
/// - It remains resilient when DTO names change but contract fields remain stable.
/// </summary>
public sealed class PromotionsContractSerializationCompatibilityTests
{
    /// <summary>
    /// Ensures diagnostics counters are serialized/deserialized with stable camelCase names used by mobile diagnostics:
    /// <c>initialCandidates</c>, <c>suppressedByFrequency</c>, <c>deduplicated</c>, <c>trimmedByCap</c>, <c>finalCount</c>.
    /// </summary>
    [Fact]
    public void PromotionsDiagnosticsContract_Should_RoundTrip_WithStableCounterPropertyNames()
    {
        // Arrange
        var diagnosticsType = FindLoyaltyContractTypeWithIntProperties(
            "InitialCandidates",
            "SuppressedByFrequency",
            "Deduplicated",
            "TrimmedByCap",
            "FinalCount");

        var instance = Activator.CreateInstance(diagnosticsType)
            ?? throw new InvalidOperationException("Failed to create promotions diagnostics contract instance.");

        SetRequiredInt32Property(diagnosticsType, instance, "InitialCandidates", 12);
        SetRequiredInt32Property(diagnosticsType, instance, "SuppressedByFrequency", 3);
        SetRequiredInt32Property(diagnosticsType, instance, "Deduplicated", 2);
        SetRequiredInt32Property(diagnosticsType, instance, "TrimmedByCap", 1);
        SetRequiredInt32Property(diagnosticsType, instance, "FinalCount", 6);

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act
        var json = JsonSerializer.Serialize(instance, diagnosticsType, options);
        var roundTripped = JsonSerializer.Deserialize(json, diagnosticsType, options);

        // Assert
        json.Should().Contain("\"initialCandidates\":12");
        json.Should().Contain("\"suppressedByFrequency\":3");
        json.Should().Contain("\"deduplicated\":2");
        json.Should().Contain("\"trimmedByCap\":1");
        json.Should().Contain("\"finalCount\":6");

        roundTripped.Should().NotBeNull();
        GetRequiredInt32Property(diagnosticsType, roundTripped!, "InitialCandidates").Should().Be(12);
        GetRequiredInt32Property(diagnosticsType, roundTripped!, "SuppressedByFrequency").Should().Be(3);
        GetRequiredInt32Property(diagnosticsType, roundTripped!, "Deduplicated").Should().Be(2);
        GetRequiredInt32Property(diagnosticsType, roundTripped!, "TrimmedByCap").Should().Be(1);
        GetRequiredInt32Property(diagnosticsType, roundTripped!, "FinalCount").Should().Be(6);
    }

    /// <summary>
    /// Ensures promotions policy contract keeps expected JSON property names and value round-trip behavior:
    /// <c>enableDeduplication</c>, <c>maxCards</c>, <c>frequencyWindowMinutes</c>, <c>suppressionWindowMinutes</c>.
    /// </summary>
    [Fact]
    public void PromotionsPolicyContract_Should_RoundTrip_WithStablePolicyPropertyNames()
    {
        // Arrange
        var policyType = FindLoyaltyContractTypeWithProperties(
            ("EnableDeduplication", typeof(bool)),
            ("MaxCards", typeof(int)),
            ("FrequencyWindowMinutes", typeof(int?)),
            ("SuppressionWindowMinutes", typeof(int?)));

        var instance = Activator.CreateInstance(policyType)
            ?? throw new InvalidOperationException("Failed to create promotions policy contract instance.");

        SetRequiredProperty(policyType, instance, "EnableDeduplication", false);
        SetRequiredProperty(policyType, instance, "MaxCards", 6);
        SetRequiredProperty(policyType, instance, "FrequencyWindowMinutes", 30);
        SetRequiredProperty(policyType, instance, "SuppressionWindowMinutes", 240);

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act
        var json = JsonSerializer.Serialize(instance, policyType, options);
        var roundTripped = JsonSerializer.Deserialize(json, policyType, options);

        // Assert
        json.Should().Contain("\"enableDeduplication\":false");
        json.Should().Contain("\"maxCards\":6");
        json.Should().Contain("\"frequencyWindowMinutes\":30");
        json.Should().Contain("\"suppressionWindowMinutes\":240");

        roundTripped.Should().NotBeNull();
        GetRequiredProperty<bool>(policyType, roundTripped!, "EnableDeduplication").Should().BeFalse();
        GetRequiredProperty<int>(policyType, roundTripped!, "MaxCards").Should().Be(6);
        GetRequiredProperty<int?>(policyType, roundTripped!, "FrequencyWindowMinutes").Should().Be(30);
        GetRequiredProperty<int?>(policyType, roundTripped!, "SuppressionWindowMinutes").Should().Be(240);
    }

    /// <summary>
    /// Finds a loyalty contract type containing all required Int32 properties.
    /// </summary>
    private static Type FindLoyaltyContractTypeWithIntProperties(params string[] propertyNames)
    {
        var contractsAssembly = typeof(ApiEnvelope<object>).Assembly;

        var match = contractsAssembly
            .GetTypes()
            .Where(t => string.Equals(t.Namespace, "Darwin.Contracts.Loyalty", StringComparison.Ordinal))
            .FirstOrDefault(t => propertyNames.All(name =>
            {
                var property = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                return property?.PropertyType == typeof(int);
            }));

        match.Should().NotBeNull("expected loyalty contract type with required integer diagnostics fields.");
        return match!;
    }

    /// <summary>
    /// Finds a loyalty contract type containing all required properties with exact expected CLR types.
    /// </summary>
    private static Type FindLoyaltyContractTypeWithProperties(params (string Name, Type Type)[] expectedProperties)
    {
        var contractsAssembly = typeof(ApiEnvelope<object>).Assembly;

        var match = contractsAssembly
            .GetTypes()
            .Where(t => string.Equals(t.Namespace, "Darwin.Contracts.Loyalty", StringComparison.Ordinal))
            .FirstOrDefault(t => expectedProperties.All(expected =>
            {
                var property = t.GetProperty(expected.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                return property is not null && property.PropertyType == expected.Type;
            }));

        match.Should().NotBeNull("expected loyalty contract type with required policy fields and CLR types.");
        return match!;
    }

    /// <summary>
    /// Sets a required Int32 property using reflection with explicit type guards.
    /// </summary>
    private static void SetRequiredInt32Property(Type ownerType, object instance, string propertyName, int value)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        property.Should().NotBeNull($"property '{propertyName}' must exist on {ownerType.FullName}.");
        property!.PropertyType.Should().Be(typeof(int), $"property '{propertyName}' must be Int32.");
        property.SetValue(instance, value);
    }

    /// <summary>
    /// Reads a required Int32 property using reflection with explicit type guards.
    /// </summary>
    private static int GetRequiredInt32Property(Type ownerType, object instance, string propertyName)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        property.Should().NotBeNull($"property '{propertyName}' must exist on {ownerType.FullName}.");
        property!.PropertyType.Should().Be(typeof(int), $"property '{propertyName}' must be Int32.");
        return (int)(property.GetValue(instance)
               ?? throw new InvalidOperationException($"Property '{propertyName}' value was null."));
    }

    /// <summary>
    /// Sets a required property using reflection with explicit type checks.
    /// </summary>
    private static void SetRequiredProperty(Type ownerType, object instance, string propertyName, object? value)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        property.Should().NotBeNull($"property '{propertyName}' must exist on {ownerType.FullName}.");
        property!.SetValue(instance, value);
    }

    /// <summary>
    /// Reads a required property using reflection and validates the target type cast.
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