using Darwin.Contracts.Identity;
using Darwin.Contracts.Invoices;
using Darwin.Contracts.Orders;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Cart;
using Darwin.Contracts.Catalog;
using Darwin.Contracts.Cms;
using Darwin.Contracts.Loyalty;
using Darwin.Contracts.Profile;
using Darwin.Contracts.Shipping;
using FluentAssertions;
using System.Text.Json;

namespace Darwin.Tests.Unit.Contracts;

/// <summary>
///     Ensures JSON payload shapes for critical contracts remain stable for mobile clients.
/// </summary>
public sealed class ContractSerializationCompatibilityProfileTests : ContractSerializationCompatibilityTestBase
{
/// <summary>
    ///     Verifies that customer profile contract can deserialize a minimal payload
    ///     while still preserving required identity fields used by profile edit flows.
    /// </summary>
    [Fact]
    public void CustomerProfile_Should_Deserialize_FromMinimalPayload()
    {
        // Arrange
        const string json = """
            {
              "id": "44444444-4444-4444-4444-444444444444",
              "email": "customer@example.test",
              "rowVersion": "AQIDBA=="
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<CustomerProfile>(json, JsonOptions);

        // Assert
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(Guid.Parse("44444444-4444-4444-4444-444444444444"));
        dto.Email.Should().Be("customer@example.test");
        dto.RowVersion.Should().NotBeNull();
        dto.RowVersion.Should().HaveCount(4);
    }

/// <summary>
    ///     Verifies that member address contracts serialize profile-facing fields
    ///     with stable camelCase property names.
    /// </summary>
    [Fact]
    public void MemberAddress_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new MemberAddress
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            RowVersion = [1, 2, 3],
            FullName = "Max Mustermann",
            Street1 = "Musterstraße 1",
            PostalCode = "10115",
            City = "Berlin",
            CountryCode = "DE",
            IsDefaultBilling = true,
            IsDefaultShipping = false
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"rowVersion\"");
        json.Should().Contain("\"fullName\"");
        json.Should().Contain("\"street1\"");
        json.Should().Contain("\"postalCode\"");
        json.Should().Contain("\"isDefaultBilling\"");
        json.Should().Contain("\"isDefaultShipping\"");
    }

/// <summary>
    ///     Verifies that member preference contracts serialize profile-facing privacy and
    ///     communication fields with stable camelCase names.
    /// </summary>
    [Fact]
    public void MemberPreferences_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new MemberPreferences
        {
            RowVersion = [1, 2, 3],
            MarketingConsent = true,
            AllowEmailMarketing = true,
            AllowSmsMarketing = false,
            AllowWhatsAppMarketing = true,
            AllowPromotionalPushNotifications = true,
            AllowOptionalAnalyticsTracking = false,
            AcceptsTermsAtUtc = new DateTime(2030, 1, 1, 10, 15, 0, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"rowVersion\"");
        json.Should().Contain("\"marketingConsent\"");
        json.Should().Contain("\"allowEmailMarketing\"");
        json.Should().Contain("\"allowSmsMarketing\"");
        json.Should().Contain("\"allowWhatsAppMarketing\"");
        json.Should().Contain("\"allowPromotionalPushNotifications\"");
        json.Should().Contain("\"allowOptionalAnalyticsTracking\"");
        json.Should().Contain("\"acceptsTermsAtUtc\"");
    }

/// <summary>
    ///     Verifies that member preference update contracts serialize mutation-facing fields
    ///     with stable camelCase names.
    /// </summary>
    [Fact]
    public void UpdateMemberPreferencesRequest_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new UpdateMemberPreferencesRequest
        {
            RowVersion = [4, 3, 2, 1],
            MarketingConsent = true,
            AllowEmailMarketing = true,
            AllowSmsMarketing = true,
            AllowWhatsAppMarketing = false,
            AllowPromotionalPushNotifications = true,
            AllowOptionalAnalyticsTracking = true
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"rowVersion\"");
        json.Should().Contain("\"marketingConsent\"");
        json.Should().Contain("\"allowEmailMarketing\"");
        json.Should().Contain("\"allowSmsMarketing\"");
        json.Should().Contain("\"allowWhatsAppMarketing\"");
        json.Should().Contain("\"allowPromotionalPushNotifications\"");
        json.Should().Contain("\"allowOptionalAnalyticsTracking\"");
    }

/// <summary>
    ///     Verifies that linked CRM customer profile contracts serialize member-facing
    ///     CRM linkage fields with stable camelCase property names.
    /// </summary>
    [Fact]
    public void LinkedCustomerProfile_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new LinkedCustomerProfile
        {
            Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            UserId = Guid.Parse("66666666-7777-8888-9999-000000000000"),
            DisplayName = "Max Mustermann",
            Email = "max@example.de",
            Phone = "+491701234567",
            CompanyName = "Darwin GmbH"
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"id\"");
        json.Should().Contain("\"userId\"");
        json.Should().Contain("\"displayName\"");
        json.Should().Contain("\"email\"");
        json.Should().Contain("\"phone\"");
        json.Should().Contain("\"companyName\"");
    }

/// <summary>
    ///     Verifies that linked CRM customer-context contracts serialize member-facing
    ///     segments, consents, and recent interactions with stable camelCase names.
    /// </summary>
    [Fact]
    public void MemberCustomerContext_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new MemberCustomerContext
        {
            Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            UserId = Guid.Parse("66666666-7777-8888-9999-000000000000"),
            DisplayName = "Max Mustermann",
            Email = "max@example.de",
            Phone = "+491701234567",
            CompanyName = "Darwin GmbH",
            Notes = "VIP coffee subscriber",
            InteractionCount = 3,
            Segments =
            [
                new MemberCustomerSegment
                {
                    SegmentId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    Name = "VIP",
                    Description = "High-value customers"
                }
            ],
            Consents =
            [
                new MemberCustomerConsent
                {
                    Id = Guid.Parse("12121212-3434-5656-7878-909090909090"),
                    Type = "MarketingEmail",
                    Granted = true,
                    GrantedAtUtc = new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc)
                }
            ],
            RecentInteractions =
            [
                new MemberCustomerInteraction
                {
                    Id = Guid.Parse("abababab-abab-abab-abab-abababababab"),
                    Type = "Support",
                    Channel = "Email",
                    Subject = "Delivery question",
                    ContentPreview = "Where is my order?",
                    CreatedAtUtc = new DateTime(2030, 1, 2, 9, 0, 0, DateTimeKind.Utc)
                }
            ]
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"displayName\"");
        json.Should().Contain("\"interactionCount\"");
        json.Should().Contain("\"segments\"");
        json.Should().Contain("\"consents\"");
        json.Should().Contain("\"recentInteractions\"");
        json.Should().Contain("\"contentPreview\"");
    }

/// <summary>
    ///     Verifies that customer profile contract serializes optimistic-concurrency and
    ///     culture fields with stable names expected by mobile profile forms.
    /// </summary>
    [Fact]
    public void CustomerProfile_Should_Serialize_WithExpectedPropertyNames()
    {
        // Arrange
        var dto = new CustomerProfile
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Email = "customer@example.test",
            FirstName = "Test",
            LastName = "Customer",
            PhoneE164 = "+491111111111",
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR",
            RowVersion = [1, 2, 3, 4]
        };

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        // Assert
        json.Should().Contain("\"id\"");
        json.Should().Contain("\"email\"");
        json.Should().Contain("\"firstName\"");
        json.Should().Contain("\"lastName\"");
        json.Should().Contain("\"phoneE164\"");
        json.Should().Contain("\"locale\"");
        json.Should().Contain("\"timezone\"");
        json.Should().Contain("\"currency\"");
        json.Should().Contain("\"rowVersion\"");
    }
}
