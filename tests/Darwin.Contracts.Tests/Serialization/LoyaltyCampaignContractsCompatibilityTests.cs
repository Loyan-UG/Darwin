using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Darwin.Contracts.Common;
using FluentAssertions;

namespace Darwin.Contracts.Tests.Serialization;

/// <summary>
/// Contract-compatibility tests for loyalty campaign DTO JSON shapes.
/// 
/// Purpose:
/// - Protect mobile business campaign editor/list flows against silent contract drift.
/// - Validate stable transport for key fields, especially optimistic-concurrency row-version tokens.
/// - Keep tests resilient to DTO type renames by using reflection over Darwin.Contracts.Loyalty.
/// </summary>
public sealed class LoyaltyCampaignContractsCompatibilityTests
{
    /// <summary>
    /// Ensures at least one loyalty campaign list/item-like DTO preserves core fields and serializes
    /// <c>RowVersion</c> as base64 string in JSON.
    /// </summary>
    [Fact]
    public void LoyaltyCampaignItemLikeContract_Should_SerializeCoreFields_AndRowVersionAsBase64()
    {
        // Arrange
        var loyaltyTypes = GetLoyaltyContractTypes();
        var itemType = FindTypeContainingProperties(loyaltyTypes,
            "Id", "BusinessId", "Name", "Title", "Channels", "TargetingJson", "PayloadJson", "RowVersion");

        itemType.Should().NotBeNull("campaign item/list contract with core fields must exist.");

        var instance = Activator.CreateInstance(itemType!)
            ?? throw new InvalidOperationException($"Could not create instance for {itemType!.FullName}.");

        var rowVersionBytes = new byte[] { 9, 8, 7, 6 };
        var expectedBase64 = Convert.ToBase64String(rowVersionBytes);

        SetIfPresent(itemType!, instance, "Id", Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        SetIfPresent(itemType!, instance, "BusinessId", Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        SetIfPresent(itemType!, instance, "Name", "campaign-spring-2026");
        SetIfPresent(itemType!, instance, "Title", "Spring 2026 Campaign");
        SetIfPresent(itemType!, instance, "Channels", ConvertToPropertyType(itemType!, "Channels", 3));
        SetIfPresent(itemType!, instance, "TargetingJson", "{\"audienceKind\":\"JoinedMembers\"}");
        SetIfPresent(itemType!, instance, "PayloadJson", "{\"cta\":\"OpenApp\"}");
        SetIfPresent(itemType!, instance, "RowVersion", rowVersionBytes);

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act
        var json = JsonSerializer.Serialize(instance, itemType!, options);
        var roundTrip = JsonSerializer.Deserialize(json, itemType!, options);

        // Assert
        json.Should().Contain("\"id\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\"");
        json.Should().Contain("\"businessId\":\"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb\"");
        json.Should().Contain("\"name\":\"campaign-spring-2026\"");
        json.Should().Contain("\"title\":\"Spring 2026 Campaign\"");
        json.Should().Contain("\"channels\":3");
        json.Should().Contain("\"targetingJson\":\"{\\u0022audienceKind\\u0022:\\u0022JoinedMembers\\u0022}\"");
        json.Should().Contain("\"payloadJson\":\"{\\u0022cta\\u0022:\\u0022OpenApp\\u0022}\"");
        json.Should().Contain($"\"rowVersion\":\"{expectedBase64}\"");

        roundTrip.Should().NotBeNull();
        var returnedRowVersion = GetRequiredProperty<byte[]>(itemType!, roundTrip!, "RowVersion");
        returnedRowVersion.Should().Equal(rowVersionBytes);
    }

    /// <summary>
    /// Ensures campaign mutation request-like contracts preserve schedule fields
    /// (<c>StartsAtUtc</c>/<c>EndsAtUtc</c>) with stable camelCase JSON names.
    /// </summary>
    [Fact]
    public void LoyaltyCampaignMutationLikeContracts_Should_ExposeStableScheduleFieldNames()
    {
        // Arrange
        var loyaltyTypes = GetLoyaltyContractTypes();
        var mutationTypes = loyaltyTypes
            .Where(t =>
                HasProperty(t, "Name") &&
                HasProperty(t, "Title") &&
                HasProperty(t, "Channels") &&
                HasProperty(t, "StartsAtUtc") &&
                HasProperty(t, "EndsAtUtc"))
            .ToList();

        mutationTypes.Should().NotBeEmpty("at least one campaign mutation DTO with schedule fields must exist.");

        var startsAtUtc = new DateTime(2026, 01, 10, 08, 30, 00, DateTimeKind.Utc);
        var endsAtUtc = new DateTime(2026, 01, 25, 21, 45, 00, DateTimeKind.Utc);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act + Assert
        foreach (var mutationType in mutationTypes)
        {
            var instance = Activator.CreateInstance(mutationType)
                ?? throw new InvalidOperationException($"Could not create instance for {mutationType.FullName}.");

            SetIfPresent(mutationType, instance, "Name", "campaign-winter");
            SetIfPresent(mutationType, instance, "Title", "Winter Campaign");
            SetIfPresent(mutationType, instance, "Channels", ConvertToPropertyType(mutationType, "Channels", 1));
            SetIfPresent(mutationType, instance, "StartsAtUtc", ConvertToPropertyType(mutationType, "StartsAtUtc", startsAtUtc));
            SetIfPresent(mutationType, instance, "EndsAtUtc", ConvertToPropertyType(mutationType, "EndsAtUtc", endsAtUtc));

            var json = JsonSerializer.Serialize(instance, mutationType, options);

            json.Should().Contain("\"startsAtUtc\"");
            json.Should().Contain("\"endsAtUtc\"");

            var roundTrip = JsonSerializer.Deserialize(json, mutationType, options);
            roundTrip.Should().NotBeNull();

            var returnedStarts = GetPropertyValueAsDateTimeUtc(mutationType, roundTrip!, "StartsAtUtc");
            var returnedEnds = GetPropertyValueAsDateTimeUtc(mutationType, roundTrip!, "EndsAtUtc");

            returnedStarts.Should().Be(startsAtUtc);
            returnedEnds.Should().Be(endsAtUtc);
        }
    }

    /// <summary>
    /// Ensures campaign update-like DTOs keep optimistic-concurrency transport fields stable:
    /// <c>Id</c> and <c>RowVersion</c> must round-trip correctly, while key editor fields remain serializable.
    /// </summary>
    [Fact]
    public void LoyaltyCampaignUpdateLikeContracts_Should_RoundTripIdAndRowVersion_WithStableJsonShape()
    {
        // Arrange
        var loyaltyTypes = GetLoyaltyContractTypes();
        var updateLikeTypes = loyaltyTypes
            .Where(t =>
                HasProperty(t, "Id") &&
                HasProperty(t, "RowVersion") &&
                HasProperty(t, "Name") &&
                HasProperty(t, "Title") &&
                HasProperty(t, "Channels"))
            .ToList();

        updateLikeTypes.Should().NotBeEmpty("at least one update-like campaign DTO must expose Id + RowVersion.");

        var id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var rowVersion = new byte[] { 5, 4, 3, 2, 1 };
        var expectedBase64 = Convert.ToBase64String(rowVersion);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act + Assert
        foreach (var dtoType in updateLikeTypes)
        {
            var instance = Activator.CreateInstance(dtoType)
                ?? throw new InvalidOperationException($"Could not create instance for {dtoType.FullName}.");

            SetIfPresent(dtoType, instance, "Id", ConvertToPropertyType(dtoType, "Id", id));
            SetIfPresent(dtoType, instance, "RowVersion", rowVersion);
            SetIfPresent(dtoType, instance, "Name", "campaign-update-like");
            SetIfPresent(dtoType, instance, "Title", "Update-like Campaign");
            SetIfPresent(dtoType, instance, "Channels", ConvertToPropertyType(dtoType, "Channels", 3));

            var json = JsonSerializer.Serialize(instance, dtoType, options);
            var roundTrip = JsonSerializer.Deserialize(json, dtoType, options);

            json.Should().Contain("\"id\":\"cccccccc-cccc-cccc-cccc-cccccccccccc\"");
            json.Should().Contain($"\"rowVersion\":\"{expectedBase64}\"");
            json.Should().Contain("\"name\":\"campaign-update-like\"");
            json.Should().Contain("\"title\":\"Update-like Campaign\"");
            json.Should().Contain("\"channels\":3");

            roundTrip.Should().NotBeNull();
            GetRequiredProperty<Guid>(dtoType, roundTrip!, "Id").Should().Be(id);
            GetRequiredProperty<byte[]>(dtoType, roundTrip!, "RowVersion").Should().Equal(rowVersion);
        }
    }

    /// <summary>
    /// Ensures create/update DTOs that model schedule as nullable fields preserve explicit null transport
    /// for <c>startsAtUtc</c> and <c>endsAtUtc</c>.
    /// </summary>
    [Fact]
    public void LoyaltyCampaignMutationLikeContracts_WithNullableSchedule_Should_SerializeNullScheduleExplicitly()
    {
        // Arrange
        var loyaltyTypes = GetLoyaltyContractTypes();
        var nullableScheduleTypes = loyaltyTypes
            .Where(t =>
                HasProperty(t, "Name") &&
                HasProperty(t, "Title") &&
                HasProperty(t, "Channels") &&
                IsNullableProperty(t, "StartsAtUtc") &&
                IsNullableProperty(t, "EndsAtUtc"))
            .ToList();

        nullableScheduleTypes.Should().NotBeEmpty(
            "at least one campaign mutation DTO should expose nullable StartsAtUtc/EndsAtUtc fields.");

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act + Assert
        foreach (var dtoType in nullableScheduleTypes)
        {
            var instance = Activator.CreateInstance(dtoType)
                ?? throw new InvalidOperationException($"Could not create instance for {dtoType.FullName}.");

            SetIfPresent(dtoType, instance, "Name", "campaign-nullable-window");
            SetIfPresent(dtoType, instance, "Title", "Nullable Window Campaign");
            SetIfPresent(dtoType, instance, "Channels", ConvertToPropertyType(dtoType, "Channels", 1));
            SetIfPresent(dtoType, instance, "StartsAtUtc", null);
            SetIfPresent(dtoType, instance, "EndsAtUtc", null);

            var json = JsonSerializer.Serialize(instance, dtoType, options);
            var roundTrip = JsonSerializer.Deserialize(json, dtoType, options);

            json.Should().Contain("\"startsAtUtc\":null");
            json.Should().Contain("\"endsAtUtc\":null");

            roundTrip.Should().NotBeNull();
            GetRawPropertyValue(dtoType, roundTrip!, "StartsAtUtc").Should().BeNull();
            GetRawPropertyValue(dtoType, roundTrip!, "EndsAtUtc").Should().BeNull();
        }
    }

    /// <summary>
    /// Ensures campaign-like contracts that expose optional content fields preserve explicit null transport.
    ///
    /// Why this matters:
    /// - Mobile business editor supports optional content sections (subtitle/body/media/landing URL).
    /// - Explicit null semantics must remain stable to avoid stale content persistence across updates.
    /// </summary>
    [Fact]
    public void LoyaltyCampaignContracts_WithOptionalContentFields_Should_SerializeNullsExplicitly()
    {
        // Arrange
        var loyaltyTypes = GetLoyaltyContractTypes();
        var optionalContentFieldNames = new[] { "Subtitle", "Body", "MediaUrl", "LandingUrl" };

        var candidateTypes = loyaltyTypes
            .Where(t =>
                HasProperty(t, "Name") &&
                HasProperty(t, "Title") &&
                optionalContentFieldNames.Any(field => IsStringLikeProperty(t, field)))
            .ToList();

        candidateTypes.Should().NotBeEmpty(
            "at least one loyalty campaign-like contract should expose optional content fields.");

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act + Assert
        foreach (var dtoType in candidateTypes)
        {
            var instance = Activator.CreateInstance(dtoType)
                ?? throw new InvalidOperationException($"Could not create instance for {dtoType.FullName}.");

            SetIfPresent(dtoType, instance, "Name", "campaign-optional-content");
            SetIfPresent(dtoType, instance, "Title", "Optional Content Campaign");

            if (HasProperty(dtoType, "Subtitle")) SetIfPresent(dtoType, instance, "Subtitle", null);
            if (HasProperty(dtoType, "Body")) SetIfPresent(dtoType, instance, "Body", null);
            if (HasProperty(dtoType, "MediaUrl")) SetIfPresent(dtoType, instance, "MediaUrl", null);
            if (HasProperty(dtoType, "LandingUrl")) SetIfPresent(dtoType, instance, "LandingUrl", null);

            var json = JsonSerializer.Serialize(instance, dtoType, options);
            var roundTrip = JsonSerializer.Deserialize(json, dtoType, options);

            if (HasProperty(dtoType, "Subtitle")) json.Should().Contain("\"subtitle\":null");
            if (HasProperty(dtoType, "Body")) json.Should().Contain("\"body\":null");
            if (HasProperty(dtoType, "MediaUrl")) json.Should().Contain("\"mediaUrl\":null");
            if (HasProperty(dtoType, "LandingUrl")) json.Should().Contain("\"landingUrl\":null");

            roundTrip.Should().NotBeNull();
            if (HasProperty(dtoType, "Subtitle")) GetRawPropertyValue(dtoType, roundTrip!, "Subtitle").Should().BeNull();
            if (HasProperty(dtoType, "Body")) GetRawPropertyValue(dtoType, roundTrip!, "Body").Should().BeNull();
            if (HasProperty(dtoType, "MediaUrl")) GetRawPropertyValue(dtoType, roundTrip!, "MediaUrl").Should().BeNull();
            if (HasProperty(dtoType, "LandingUrl")) GetRawPropertyValue(dtoType, roundTrip!, "LandingUrl").Should().BeNull();
        }
    }

    /// <summary>
    /// Ensures campaign item-like contracts preserve lifecycle state field transport.
    ///
    /// Why this matters:
    /// - Mobile operations screens rely on campaign-state badges and filters.
    /// - Contract drift in <c>CampaignState</c> naming/value transport can break state UI behavior.
    /// </summary>
    [Fact]
    public void LoyaltyCampaignItemLikeContracts_WithCampaignState_Should_RoundTripStateValue()
    {
        // Arrange
        var loyaltyTypes = GetLoyaltyContractTypes();
        var stateAwareTypes = loyaltyTypes
            .Where(t =>
                HasProperty(t, "Name") &&
                HasProperty(t, "Title") &&
                IsStringLikeProperty(t, "CampaignState"))
            .ToList();

        stateAwareTypes.Should().NotBeEmpty(
            "at least one campaign item-like contract should expose CampaignState for mobile filtering.");

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act + Assert
        foreach (var dtoType in stateAwareTypes)
        {
            var instance = Activator.CreateInstance(dtoType)
                ?? throw new InvalidOperationException($"Could not create instance for {dtoType.FullName}.");

            SetIfPresent(dtoType, instance, "Name", "campaign-stateful");
            SetIfPresent(dtoType, instance, "Title", "Stateful Campaign");
            SetIfPresent(dtoType, instance, "CampaignState", "Active");

            var json = JsonSerializer.Serialize(instance, dtoType, options);
            var roundTrip = JsonSerializer.Deserialize(json, dtoType, options);

            json.Should().Contain("\"campaignState\":\"Active\"");
            roundTrip.Should().NotBeNull();
            GetRawPropertyValue(dtoType, roundTrip!, "CampaignState")?.ToString().Should().Be("Active");
        }
    }

    /// <summary>
    /// Ensures campaign contracts that expose JSON-string fields keep exact payload text semantics
    /// for <c>TargetingJson</c> and <c>PayloadJson</c> during round-trip transport.
    ///
    /// Why this matters:
    /// - Business editor sends these fields as raw JSON strings.
    /// - Any contract drift or non-string normalization may break server-side parsing guarantees.
    /// </summary>
    [Fact]
    public void LoyaltyCampaignContracts_WithTargetingAndPayloadJson_Should_RoundTripRawJsonStrings()
    {
        // Arrange
        var loyaltyTypes = GetLoyaltyContractTypes();
        var jsonStringAwareTypes = loyaltyTypes
            .Where(t =>
                IsStringLikeProperty(t, "TargetingJson") &&
                IsStringLikeProperty(t, "PayloadJson"))
            .ToList();

        jsonStringAwareTypes.Should().NotBeEmpty(
            "at least one campaign contract should expose TargetingJson and PayloadJson string fields.");

        const string targetingJson = "{\"audienceKind\":\"PointsThreshold\",\"minimumPoints\":120}";
        const string payloadJson = "{\"cta\":\"OpenApp\",\"deepLink\":\"darwin://rewards\"}";
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act + Assert
        foreach (var dtoType in jsonStringAwareTypes)
        {
            var instance = Activator.CreateInstance(dtoType)
                ?? throw new InvalidOperationException($"Could not create instance for {dtoType.FullName}.");

            SetIfPresent(dtoType, instance, "TargetingJson", targetingJson);
            SetIfPresent(dtoType, instance, "PayloadJson", payloadJson);

            var json = JsonSerializer.Serialize(instance, dtoType, options);
            var roundTrip = JsonSerializer.Deserialize(json, dtoType, options);

            json.Should().Contain("\"targetingJson\"");
            json.Should().Contain("\"payloadJson\"");

            roundTrip.Should().NotBeNull();
            GetRawPropertyValue(dtoType, roundTrip!, "TargetingJson")?.ToString().Should().Be(targetingJson);
            GetRawPropertyValue(dtoType, roundTrip!, "PayloadJson")?.ToString().Should().Be(payloadJson);
        }
    }

    /// <summary>
    /// Ensures channel field transport remains stable for supported values used by mobile editor
    /// (In-App only = 1, In-App + Push = 3).
    /// </summary>
    [Fact]
    public void LoyaltyCampaignContracts_WithChannels_Should_RoundTripSupportedChannelValues()
    {
        // Arrange
        var loyaltyTypes = GetLoyaltyContractTypes();
        var channelAwareTypes = loyaltyTypes
            .Where(t => HasProperty(t, "Channels"))
            .ToList();

        channelAwareTypes.Should().NotBeEmpty(
            "at least one campaign contract should expose Channels.");

        var supportedValues = new[] { 1, 3 };
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act + Assert
        foreach (var dtoType in channelAwareTypes)
        {
            foreach (var channelValue in supportedValues)
            {
                var instance = Activator.CreateInstance(dtoType)
                    ?? throw new InvalidOperationException($"Could not create instance for {dtoType.FullName}.");

                SetIfPresent(dtoType, instance, "Channels", ConvertToPropertyType(dtoType, "Channels", channelValue));

                var json = JsonSerializer.Serialize(instance, dtoType, options);
                var roundTrip = JsonSerializer.Deserialize(json, dtoType, options);

                json.Should().Contain($"\"channels\":{channelValue}");
                roundTrip.Should().NotBeNull();
                Convert.ToInt32(GetRawPropertyValue(dtoType, roundTrip!, "Channels"))
                    .Should().Be(channelValue);
            }
        }
    }

    /// <summary>
    /// Ensures contracts carrying <c>RowVersion</c> can explicitly serialize null in scenarios
    /// where concurrency token is intentionally absent in request payload composition.
    /// </summary>
    [Fact]
    public void LoyaltyCampaignContracts_WithRowVersion_Should_SerializeNullRowVersionExplicitly_WhenUnset()
    {
        // Arrange
        var loyaltyTypes = GetLoyaltyContractTypes();
        var rowVersionTypes = loyaltyTypes
            .Where(t => IsByteArrayProperty(t, "RowVersion"))
            .ToList();

        rowVersionTypes.Should().NotBeEmpty(
            "at least one campaign contract should expose RowVersion byte[] field.");

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act + Assert
        foreach (var dtoType in rowVersionTypes)
        {
            var instance = Activator.CreateInstance(dtoType)
                ?? throw new InvalidOperationException($"Could not create instance for {dtoType.FullName}.");

            SetIfPresent(dtoType, instance, "RowVersion", null);

            var json = JsonSerializer.Serialize(instance, dtoType, options);
            var roundTrip = JsonSerializer.Deserialize(json, dtoType, options);

            json.Should().Contain("\"rowVersion\":null");
            roundTrip.Should().NotBeNull();
            GetRawPropertyValue(dtoType, roundTrip!, "RowVersion").Should().BeNull();
        }
    }

    /// <summary>
    /// Ensures campaign-like contracts that expose <c>BusinessId</c> keep stable GUID transport.
    ///
    /// Why this matters:
    /// - Mobile business flows scope campaign operations by business identifier.
    /// - Contract drift on GUID transport would break request routing and list filtering.
    /// </summary>
    [Fact]
    public void LoyaltyCampaignContracts_WithBusinessId_Should_RoundTripGuidValue()
    {
        // Arrange
        var loyaltyTypes = GetLoyaltyContractTypes();
        var businessScopedTypes = loyaltyTypes
            .Where(t => IsGuidLikeProperty(t, "BusinessId"))
            .ToList();

        businessScopedTypes.Should().NotBeEmpty(
            "at least one campaign contract should expose BusinessId for business-scoped operations.");

        var businessId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act + Assert
        foreach (var dtoType in businessScopedTypes)
        {
            var instance = Activator.CreateInstance(dtoType)
                ?? throw new InvalidOperationException($"Could not create instance for {dtoType.FullName}.");

            SetIfPresent(dtoType, instance, "BusinessId", ConvertToPropertyType(dtoType, "BusinessId", businessId));

            var json = JsonSerializer.Serialize(instance, dtoType, options);
            var roundTrip = JsonSerializer.Deserialize(json, dtoType, options);

            json.Should().Contain("\"businessId\":\"11111111-2222-3333-4444-555555555555\"");

            roundTrip.Should().NotBeNull();
            ReadGuidLikePropertyValue(dtoType, roundTrip!, "BusinessId").Should().Be(businessId);
        }
    }

    /// <summary>
    /// Ensures campaign schedule transport remains value-preserving even when the range is logically invalid
    /// (<c>StartsAtUtc</c> after <c>EndsAtUtc</c>), because DTO contracts should not mutate payload values.
    /// Validation/range enforcement belongs to application/domain layers.
    /// </summary>
    [Fact]
    public void LoyaltyCampaignContracts_WithScheduleFields_Should_PreserveRawRangeValues()
    {
        // Arrange
        var loyaltyTypes = GetLoyaltyContractTypes();
        var scheduleTypes = loyaltyTypes
            .Where(t => HasProperty(t, "StartsAtUtc") && HasProperty(t, "EndsAtUtc"))
            .ToList();

        scheduleTypes.Should().NotBeEmpty(
            "at least one campaign contract should expose StartsAtUtc/EndsAtUtc.");

        var startsAtUtc = new DateTime(2026, 02, 10, 15, 00, 00, DateTimeKind.Utc);
        var endsAtUtc = new DateTime(2026, 02, 01, 08, 30, 00, DateTimeKind.Utc); // intentionally earlier
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act + Assert
        foreach (var dtoType in scheduleTypes)
        {
            var instance = Activator.CreateInstance(dtoType)
                ?? throw new InvalidOperationException($"Could not create instance for {dtoType.FullName}.");

            SetIfPresent(dtoType, instance, "StartsAtUtc", ConvertToPropertyType(dtoType, "StartsAtUtc", startsAtUtc));
            SetIfPresent(dtoType, instance, "EndsAtUtc", ConvertToPropertyType(dtoType, "EndsAtUtc", endsAtUtc));

            var json = JsonSerializer.Serialize(instance, dtoType, options);
            var roundTrip = JsonSerializer.Deserialize(json, dtoType, options);

            json.Should().Contain("\"startsAtUtc\"");
            json.Should().Contain("\"endsAtUtc\"");

            roundTrip.Should().NotBeNull();
            GetPropertyValueAsDateTimeUtc(dtoType, roundTrip!, "StartsAtUtc").Should().Be(startsAtUtc);
            GetPropertyValueAsDateTimeUtc(dtoType, roundTrip!, "EndsAtUtc").Should().Be(endsAtUtc);
        }
    }

    /// <summary>
    /// Ensures contracts exposing campaign identity fields preserve stable Guid transport
    /// for both <c>Id</c> and <c>BusinessId</c> when both properties are present.
    /// </summary>
    [Fact]
    public void LoyaltyCampaignContracts_WithIdAndBusinessId_Should_RoundTripBothGuids()
    {
        // Arrange
        var loyaltyTypes = GetLoyaltyContractTypes();
        var identityTypes = loyaltyTypes
            .Where(t => HasProperty(t, "Id") && HasProperty(t, "BusinessId"))
            .ToList();

        identityTypes.Should().NotBeEmpty(
            "at least one campaign contract should expose both Id and BusinessId.");

        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var businessId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act + Assert
        foreach (var dtoType in identityTypes)
        {
            var instance = Activator.CreateInstance(dtoType)
                ?? throw new InvalidOperationException($"Could not create instance for {dtoType.FullName}.");

            SetIfPresent(dtoType, instance, "Id", ConvertToPropertyType(dtoType, "Id", id));
            SetIfPresent(dtoType, instance, "BusinessId", ConvertToPropertyType(dtoType, "BusinessId", businessId));

            var json = JsonSerializer.Serialize(instance, dtoType, options);
            var roundTrip = JsonSerializer.Deserialize(json, dtoType, options);

            json.Should().Contain("\"id\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\"");
            json.Should().Contain("\"businessId\":\"11111111-2222-3333-4444-555555555555\"");

            roundTrip.Should().NotBeNull();
            ReadGuidLikePropertyValue(dtoType, roundTrip!, "Id").Should().Be(id);
            ReadGuidLikePropertyValue(dtoType, roundTrip!, "BusinessId").Should().Be(businessId);
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

    private static Type? FindTypeContainingProperties(IEnumerable<Type> candidates, params string[] properties)
    {
        return candidates.FirstOrDefault(t => properties.All(p => HasProperty(t, p)));
    }

    private static bool HasProperty(Type ownerType, string propertyName) =>
        ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) is not null;

    private static bool IsNullableProperty(Type ownerType, string propertyName)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property is null)
        {
            return false;
        }

        var type = property.PropertyType;
        return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
    }

    private static bool IsStringLikeProperty(Type ownerType, string propertyName)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property is null)
        {
            return false;
        }

        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        return type == typeof(string);
    }

    private static bool IsByteArrayProperty(Type ownerType, string propertyName)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property is null)
        {
            return false;
        }

        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        return type == typeof(byte[]);
    }

    private static bool IsGuidLikeProperty(Type ownerType, string propertyName)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property is null)
        {
            return false;
        }

        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        return type == typeof(Guid);
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

    private static object? GetRawPropertyValue(Type ownerType, object instance, string propertyName)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        property.Should().NotBeNull($"property '{propertyName}' must exist on {ownerType.FullName}.");
        return property!.GetValue(instance);
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

        // DateTime -> DateTimeOffset conversion support for schedule-like fields.
        if (targetType == typeof(DateTimeOffset) && value is DateTime dt)
        {
            return new DateTimeOffset(dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime());
        }

        return Convert.ChangeType(value, targetType);
    }

    private static Guid ReadGuidLikePropertyValue(Type ownerType, object instance, string propertyName)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on {ownerType.FullName}.");

        var raw = property.GetValue(instance);

        if (raw is Guid guid)
        {
            return guid;
        }

        throw new InvalidOperationException($"Property '{propertyName}' was null or not Guid-compatible.");
    }

    private static DateTime GetPropertyValueAsDateTimeUtc(Type ownerType, object instance, string propertyName)
    {
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on {ownerType.FullName}.");

        var raw = property.GetValue(instance);

        if (raw is DateTime dt)
        {
            return dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
        }

        if (raw is DateTimeOffset dto)
        {
            return dto.UtcDateTime;
        }

        throw new InvalidOperationException($"Property '{propertyName}' was null or not DateTime-compatible.");
    }
}