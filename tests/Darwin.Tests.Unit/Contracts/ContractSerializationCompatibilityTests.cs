using Darwin.Contracts.Identity;
using Darwin.Contracts.Loyalty;
using Darwin.Contracts.Profile;
using FluentAssertions;
using System.Text.Json;

namespace Darwin.Tests.Unit.Contracts;

/// <summary>
///     Ensures JSON payload shapes for critical contracts remain stable for mobile clients.
///     These tests are intentionally focused on serialized property names to detect accidental
///     breaking changes introduced by refactors or serializer-option drift.
/// </summary>
public sealed class ContractSerializationCompatibilityTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
    ///     Verifies that prepare-scan response keeps the expected field names for session token,
    ///     mode, expiration, current balance, and selected rewards list.
    /// </summary>
    [Fact]
    public void PrepareScanSessionResponse_Should_Serialize_WithExpectedPropertyNames()
    {
        // Arrange
        var dto = new PrepareScanSessionResponse
        {
            ScanSessionToken = "opaque-session-token",
            Mode = LoyaltyScanMode.Redemption,
            ExpiresAtUtc = new DateTime(2030, 2, 1, 12, 0, 0, DateTimeKind.Utc),
            CurrentPointsBalance = 320,
            SelectedRewards =
            [
                new LoyaltyRewardSummary
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    BusinessId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "Free Coffee",
                    Description = "One medium cup",
                    RequiredPoints = 100,
                    IsActive = true,
                    IsSelectable = true
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        // Assert
        json.Should().Contain("\"scanSessionToken\"");
        json.Should().Contain("\"mode\"");
        json.Should().Contain("\"expiresAtUtc\"");
        json.Should().Contain("\"currentPointsBalance\"");
        json.Should().Contain("\"selectedRewards\"");
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
