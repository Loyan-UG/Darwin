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
            DeviceId = "device-1"
        };

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        // Assert
        json.Should().Contain("\"email\"");
        json.Should().Contain("\"password\"");
        json.Should().Contain("\"deviceId\"");
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
    ///     Verifies that business-process response contract serializes session mode,
    ///     account summary, selected rewards and allowed actions with stable names.
    /// </summary>
    [Fact]
    public void ProcessScanSessionForBusinessResponse_Should_Serialize_WithExpectedPropertyNames()
    {
        // Arrange
        var dto = new ProcessScanSessionForBusinessResponse
        {
            Mode = LoyaltyScanMode.Accrual,
            BusinessId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            BusinessLocationId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            CustomerDisplayName = "Member #104",
            AllowedActions = LoyaltyScanAllowedActions.CanAccruePoints,
            AccountSummary = new BusinessLoyaltyAccountSummary
            {
                LoyaltyAccountId = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                PointsBalance = 200,
                CustomerDisplayName = "Member #104"
            },
            SelectedRewards = []
        };

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        // Assert
        json.Should().Contain("\"mode\"");
        json.Should().Contain("\"businessId\"");
        json.Should().Contain("\"businessLocationId\"");
        json.Should().Contain("\"accountSummary\"");
        json.Should().Contain("\"customerDisplayName\"");
        json.Should().Contain("\"selectedRewards\"");
        json.Should().Contain("\"allowedActions\"");
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
    ///     Verifies that confirm-redemption response keeps expected field names
    ///     for success indicators, balance, account snapshot and error details.
    /// </summary>
    [Fact]
    public void ConfirmRedemptionResponse_Should_Serialize_WithExpectedPropertyNames()
    {
        // Arrange
        var dto = new ConfirmRedemptionResponse
        {
            Success = true,
            NewBalance = 120,
            UpdatedAccount = new LoyaltyAccountSummary
            {
                LoyaltyAccountId = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                BusinessId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                BusinessName = "Coffee Shop",
                PointsBalance = 120,
                LifetimePoints = 480,
                Status = "Active",
                LastAccrualAtUtc = null,
                NextRewardTitle = null
            },
            ErrorCode = null,
            ErrorMessage = null
        };

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        // Assert
        json.Should().Contain(""success"");
        json.Should().Contain(""newBalance"");
        json.Should().Contain(""updatedAccount"");
        json.Should().Contain(""errorCode"");
        json.Should().Contain(""errorMessage"");
    }

    /// <summary>
    ///     Verifies that confirm-redemption response can be deserialized from
    ///     a failure payload while preserving error code/message fields.
    /// </summary>
    [Fact]
    public void ConfirmRedemptionResponse_Should_Deserialize_WithFailurePayload()
    {
        // Arrange
        const string json = """
            {
              "success": false,
              "newBalance": null,
              "updatedAccount": null,
              "errorCode": "SESSION_EXPIRED",
              "errorMessage": "Session has expired."
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<ConfirmRedemptionResponse>(json, JsonOptions);

        // Assert
        dto.Should().NotBeNull();
        dto!.Success.Should().BeFalse();
        dto.NewBalance.Should().BeNull();
        dto.UpdatedAccount.Should().BeNull();
        dto.ErrorCode.Should().Be("SESSION_EXPIRED");
        dto.ErrorMessage.Should().Be("Session has expired.");
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
