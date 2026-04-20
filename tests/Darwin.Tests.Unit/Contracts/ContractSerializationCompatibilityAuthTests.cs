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
public sealed class ContractSerializationCompatibilityAuthTests : ContractSerializationCompatibilityTestBase
{
/// <summary>
    ///     Verifies that token response contract serializes core authentication fields using
    ///     the expected camelCase names consumed by clients.
    /// </summary>
    [Fact]
    public void TokenResponse_Should_Serialize_WithExpectedPropertyNames()
    {
        // Arrange
        var dto = new TokenResponse
        {
            AccessToken = "access-token-value",
            AccessTokenExpiresAtUtc = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            RefreshToken = "refresh-token-value",
            RefreshTokenExpiresAtUtc = new DateTime(2030, 1, 8, 0, 0, 0, DateTimeKind.Utc),
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "member@example.test",
            Scopes = new[] { "profile.read", "loyalty.use" }
        };

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        // Assert
        json.Should().Contain("\"accessToken\"");
        json.Should().Contain("\"accessTokenExpiresAtUtc\"");
        json.Should().Contain("\"refreshToken\"");
        json.Should().Contain("\"refreshTokenExpiresAtUtc\"");
        json.Should().Contain("\"userId\"");
        json.Should().Contain("\"email\"");
        json.Should().Contain("\"scopes\"");
    }

/// <summary>
    ///     Verifies that login request contract keeps expected camelCase field names.
    ///     This protects authentication call compatibility across mobile clients.
    /// </summary>
    [Fact]
    public void PasswordLoginRequest_Should_Serialize_WithExpectedPropertyNames()
    {
        // Arrange
        var dto = new PasswordLoginRequest
        {
            Email = "member@example.test",
            Password = "SecurePassword123!",
            DeviceId = "device-1",
            BusinessId = Guid.Parse("12121212-3434-5656-7878-909090909090")
        };

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        // Assert
        json.Should().Contain("\"email\"");
        json.Should().Contain("\"password\"");
        json.Should().Contain("\"deviceId\"");
        json.Should().Contain("\"businessId\"");
    }

/// <summary>
    ///     Verifies that refresh request contract preserves device binding and preferred business
    ///     context field names for business-app token refresh.
    /// </summary>
    [Fact]
    public void RefreshTokenRequest_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new RefreshTokenRequest
        {
            RefreshToken = "refresh-token-value",
            DeviceId = "device-1",
            BusinessId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"refreshToken\"");
        json.Should().Contain("\"deviceId\"");
        json.Should().Contain("\"businessId\"");
    }

/// <summary>
    ///     Verifies that email-confirmation request contracts preserve stable camelCase fields
    ///     for activation and resend flows.
    /// </summary>
    [Fact]
    public void EmailConfirmationRequests_Should_Serialize_WithExpectedPropertyNames()
    {
        var request = new RequestEmailConfirmationRequest
        {
            Email = "member@example.test"
        };

        var confirm = new ConfirmEmailRequest
        {
            Email = "member@example.test",
            Token = "confirm-token-123"
        };

        var requestJson = JsonSerializer.Serialize(request, JsonOptions);
        var confirmJson = JsonSerializer.Serialize(confirm, JsonOptions);

        requestJson.Should().Contain("\"email\"");
        confirmJson.Should().Contain("\"email\"");
        confirmJson.Should().Contain("\"token\"");
    }

/// <summary>
    ///     Verifies that business invitation preview contracts serialize onboarding payload fields
    ///     with stable camelCase names for business-mobile clients.
    /// </summary>
    [Fact]
    public void BusinessInvitationPreviewResponse_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new BusinessInvitationPreviewResponse
        {
            InvitationId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            BusinessId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            BusinessName = "Cafe Morgenrot",
            Email = "operator@morgenrot.de",
            Role = "Owner",
            Status = "Pending",
            ExpiresAtUtc = new DateTime(2030, 1, 2, 10, 0, 0, DateTimeKind.Utc),
            HasExistingUser = false
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"invitationId\"");
        json.Should().Contain("\"businessId\"");
        json.Should().Contain("\"businessName\"");
        json.Should().Contain("\"email\"");
        json.Should().Contain("\"role\"");
        json.Should().Contain("\"status\"");
        json.Should().Contain("\"expiresAtUtc\"");
        json.Should().Contain("\"hasExistingUser\"");
    }

/// <summary>
    ///     Verifies that business invitation acceptance request preserves the token-entry onboarding
    ///     payload field names expected by the business-mobile app.
    /// </summary>
    [Fact]
    public void AcceptBusinessInvitationRequest_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new AcceptBusinessInvitationRequest
        {
            Token = "invite-token",
            DeviceId = "device-1",
            FirstName = "Greta",
            LastName = "Sommer",
            Password = "Business123!"
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"token\"");
        json.Should().Contain("\"deviceId\"");
        json.Should().Contain("\"firstName\"");
        json.Should().Contain("\"lastName\"");
        json.Should().Contain("\"password\"");
    }

/// <summary>
    ///     Verifies that token response contract can be deserialized from a payload
    ///     containing both known and unknown fields, preserving forward compatibility
    ///     when server adds non-breaking fields.
    /// </summary>
    [Fact]
    public void TokenResponse_Should_Deserialize_WhenUnknownFieldsArePresent()
    {
        // Arrange
        const string json = """
            {
              "accessToken": "access-token",
              "accessTokenExpiresAtUtc": "2030-01-01T00:00:00Z",
              "refreshToken": "refresh-token",
              "refreshTokenExpiresAtUtc": "2030-01-08T00:00:00Z",
              "userId": "11111111-1111-1111-1111-111111111111",
              "email": "member@example.test",
              "unexpectedFutureField": "ignored"
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<TokenResponse>(json, JsonOptions);

        // Assert
        dto.Should().NotBeNull();
        dto!.AccessToken.Should().Be("access-token");
        dto.RefreshToken.Should().Be("refresh-token");
        dto.Email.Should().Be("member@example.test");
    }
}
