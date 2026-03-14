using Darwin.Contracts.Identity;
using Darwin.Contracts.Businesses;
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
    ///     Verifies that process-scan request contract serializes the scan token
    ///     field with expected camelCase name.
    /// </summary>
    [Fact]
    public void ProcessScanSessionForBusinessRequest_Should_Serialize_WithExpectedPropertyNames()
    {
        // Arrange
        var dto = new ProcessScanSessionForBusinessRequest
        {
            ScanSessionToken = "opaque-token-value"
        };

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        // Assert
        json.Should().Contain("\"scanSessionToken\"");
    }

    /// <summary>
    ///     Verifies that confirm-redemption request contract serializes the scan token
    ///     field with expected camelCase name.
    /// </summary>
    [Fact]
    public void ConfirmRedemptionRequest_Should_Serialize_WithExpectedPropertyNames()
    {
        // Arrange
        var dto = new ConfirmRedemptionRequest
        {
            ScanSessionToken = "opaque-token-value"
        };

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        // Assert
        json.Should().Contain("\"scanSessionToken\"");
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
                    LoyaltyRewardTierId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
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
    ///     Verifies that confirm-accrual response keeps expected field names
    ///     for success indicators, balance, account snapshot and error details.
    /// </summary>
    [Fact]
    public void ConfirmAccrualResponse_Should_Serialize_WithExpectedPropertyNames()
    {
        // Arrange
        var dto = new ConfirmAccrualResponse
        {
            Success = true,
            NewBalance = 240,
            UpdatedAccount = new LoyaltyAccountSummary
            {
                LoyaltyAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                BusinessId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                BusinessName = "Bakery",
                PointsBalance = 240,
                LifetimePoints = 860,
                Status = "Active",
                LastAccrualAtUtc = new DateTime(2030, 5, 1, 8, 0, 0, DateTimeKind.Utc),
                NextRewardTitle = "Free Bread"
            },
            ErrorCode = null,
            ErrorMessage = null
        };

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        // Assert
        json.Should().Contain("\"success\"");
        json.Should().Contain("\"newBalance\"");
        json.Should().Contain("\"updatedAccount\"");
        json.Should().Contain("\"errorCode\"");
        json.Should().Contain("\"errorMessage\"");
    }

    /// <summary>
    ///     Verifies that confirm-accrual response can be deserialized from
    ///     a failure payload while preserving error code/message fields.
    /// </summary>
    [Fact]
    public void ConfirmAccrualResponse_Should_Deserialize_WithFailurePayload()
    {
        // Arrange
        const string json = """
            {
              "success": false,
              "newBalance": null,
              "updatedAccount": null,
              "errorCode": "ACCOUNT_LOCKED",
              "errorMessage": "Account is locked."
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<ConfirmAccrualResponse>(json, JsonOptions);

        // Assert
        dto.Should().NotBeNull();
        dto!.Success.Should().BeFalse();
        dto.NewBalance.Should().BeNull();
        dto.UpdatedAccount.Should().BeNull();
        dto.ErrorCode.Should().Be("ACCOUNT_LOCKED");
        dto.ErrorMessage.Should().Be("Account is locked.");
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
        json.Should().Contain("\"success\"");
        json.Should().Contain("\"newBalance\"");
        json.Should().Contain("\"updatedAccount\"");
        json.Should().Contain("\"errorCode\"");
        json.Should().Contain("\"errorMessage\"");
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

    /// <summary>
    ///     Verifies that promotions feed response serializes campaign-oriented fields
    ///     with expected camelCase names used by mobile feed rendering.
    /// </summary>
    [Fact]
    public void MyPromotionsResponse_Should_Serialize_WithExpectedPropertyNames()
    {
        // Arrange
        var dto = new MyPromotionsResponse
        {
            AppliedPolicy = new PromotionFeedPolicy
            {
                EnableDeduplication = true,
                MaxCards = 6,
                SuppressionWindowMinutes = 480
            },
            Items =
            [
                new PromotionFeedItem
                {
                    BusinessId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                    BusinessName = "Demo Coffee",
                    Title = "Morning Bonus",
                    Description = "Earn extra points before noon.",
                    CtaKind = "OpenRewards",
                    Priority = 20,
                    CampaignId = Guid.Parse("cccccccc-1111-2222-3333-dddddddddddd"),
                    CampaignState = PromotionCampaignState.Active,
                    StartsAtUtc = new DateTime(2030, 6, 1, 8, 0, 0, DateTimeKind.Utc),
                    EndsAtUtc = new DateTime(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc),
                    EligibilityRules =
                    [
                        new PromotionEligibilityRule
                        {
                            AudienceKind = PromotionAudienceKind.PointsThreshold,
                            MinPoints = 100,
                            MaxPoints = null,
                            TierKey = null,
                            Note = "Available for members with at least 100 points."
                        }
                    ]
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        // Assert
        json.Should().Contain("\"items\"");
        json.Should().Contain("\"appliedPolicy\"");
        json.Should().Contain("\"enableDeduplication\"");
        json.Should().Contain("\"maxCards\"");
        json.Should().Contain("\"suppressionWindowMinutes\"");
        json.Should().Contain("\"campaignId\"");
        json.Should().Contain("\"campaignState\"");
        json.Should().Contain("\"startsAtUtc\"");
        json.Should().Contain("\"endsAtUtc\"");
        json.Should().Contain("\"eligibilityRules\"");
        json.Should().Contain("\"audienceKind\"");
        json.Should().Contain("\"minPoints\"");
    }

    /// <summary>
    ///     Verifies that loyalty timeline response serializes cursor fields and nested
    ///     entries with expected property names for mobile infinite-scroll UX.
    /// </summary>
    [Fact]
    public void GetMyLoyaltyTimelinePageResponse_Should_Serialize_WithExpectedPropertyNames()
    {
        // Arrange
        var dto = new GetMyLoyaltyTimelinePageResponse
        {
            NextBeforeAtUtc = new DateTime(2030, 7, 20, 10, 30, 0, DateTimeKind.Utc),
            NextBeforeId = Guid.Parse("10101010-2020-3030-4040-505050505050"),
            Items =
            [
                new LoyaltyTimelineEntry
                {
                    Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    Kind = LoyaltyTimelineEntryKind.PointsTransaction,
                    LoyaltyAccountId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    BusinessId = Guid.Parse("99999999-8888-7777-6666-555555555555"),
                    OccurredAtUtc = new DateTime(2030, 7, 20, 11, 0, 0, DateTimeKind.Utc),
                    PointsDelta = 5,
                    PointsSpent = null,
                    RewardTierId = null,
                    Reference = "Accrual",
                    Note = "Seed timeline entry"
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        // Assert
        json.Should().Contain("\"items\"");
        json.Should().Contain("\"nextBeforeAtUtc\"");
        json.Should().Contain("\"nextBeforeId\"");
        json.Should().Contain("\"occurredAtUtc\"");
        json.Should().Contain("\"pointsDelta\"");
        json.Should().Contain("\"pointsSpent\"");
        json.Should().Contain("\"reference\"");
    }

    /// <summary>
    ///     Verifies that map discovery request serializes viewport bounds and filters
    ///     with expected camelCase names consumed by discovery endpoints.
    /// </summary>
    [Fact]
    public void BusinessMapDiscoveryRequest_Should_Serialize_WithExpectedPropertyNames()
    {
        // Arrange
        var dto = new BusinessMapDiscoveryRequest
        {
            Bounds = new Darwin.Contracts.Common.GeoBoundsModel
            {
                NorthLat = 52.6,
                SouthLat = 52.4,
                EastLon = 13.5,
                WestLon = 13.2
            },
            Page = 1,
            PageSize = 50,
            Category = "Cafe",
            Query = "coffee",
            CountryCode = "DE"
        };

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        // Assert
        json.Should().Contain("\"bounds\"");
        json.Should().Contain("\"northLat\"");
        json.Should().Contain("\"southLat\"");
        json.Should().Contain("\"eastLon\"");
        json.Should().Contain("\"westLon\"");
        json.Should().Contain("\"page\"");
        json.Should().Contain("\"pageSize\"");
        json.Should().Contain("\"category\"");
        json.Should().Contain("\"query\"");
        json.Should().Contain("\"countryCode\"");
    }

    /// <summary>
    ///     Verifies that business summary contract deserializes from payload containing
    ///     proximity fields used by mobile explore/discovery screens.
    /// </summary>
    [Fact]
    public void BusinessSummary_Should_Deserialize_WithLocationAndDistanceFields()
    {
        // Arrange
        const string json = """
            {
              "id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
              "name": "Coffee Hub",
              "category": "Cafe",
              "rating": 4.7,
              "ratingCount": 132,
              "location": {
                "latitude": 52.520008,
                "longitude": 13.404954,
                "altitudeMeters": null
              },
              "city": "Berlin",
              "isOpenNow": true,
              "isActive": true,
              "distanceMeters": 380,
              "futureField": "ignored"
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<BusinessSummary>(json, JsonOptions);

        // Assert
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        dto.Name.Should().Be("Coffee Hub");
        dto.Category.Should().Be("Cafe");
        dto.Location.Should().NotBeNull();
        dto.Location!.Latitude.Should().Be(52.520008);
        dto.Location.Longitude.Should().Be(13.404954);
        dto.DistanceMeters.Should().Be(380);
        dto.IsOpenNow.Should().BeTrue();
    }
    /// <summary>
    ///     Verifies that business campaign create/update contracts serialize mutable
    ///     payload fields and optimistic-concurrency token with stable camelCase names.
    /// </summary>
    [Fact]
    public void BusinessCampaignRequests_Should_Serialize_WithExpectedPropertyNames()
    {
        // Arrange
        var create = new CreateBusinessCampaignRequest
        {
            Name = "Weekend Boost",
            Title = "Double Points",
            Subtitle = "Fri-Sun",
            Body = "Earn 2x points.",
            MediaUrl = "https://cdn.example/campaign.jpg",
            LandingUrl = "https://example.test/rewards",
            Channels = 3,
            StartsAtUtc = new DateTime(2030, 8, 1, 8, 0, 0, DateTimeKind.Utc),
            EndsAtUtc = new DateTime(2030, 8, 3, 20, 0, 0, DateTimeKind.Utc),
            TargetingJson = "{\"joined\":true}",
            PayloadJson = "{\"kind\":\"boost\"}"
        };

        var update = new UpdateBusinessCampaignRequest
        {
            Id = Guid.Parse("12121212-3434-5656-7878-909090909090"),
            Name = "Weekend Boost v2",
            Title = "Triple Points",
            Channels = 1,
            TargetingJson = "{\"tier\":\"gold\"}",
            PayloadJson = "{\"kind\":\"boost-v2\"}",
            RowVersion = [1, 2, 3]
        };

        // Act
        var createJson = JsonSerializer.Serialize(create, JsonOptions);
        var updateJson = JsonSerializer.Serialize(update, JsonOptions);

        // Assert
        createJson.Should().Contain("\"name\"");
        createJson.Should().Contain("\"title\"");
        createJson.Should().Contain("\"channels\"");
        createJson.Should().Contain("\"targetingJson\"");
        createJson.Should().Contain("\"payloadJson\"");
        createJson.Should().Contain("\"startsAtUtc\"");
        createJson.Should().Contain("\"endsAtUtc\"");

        updateJson.Should().Contain("\"id\"");
        updateJson.Should().Contain("\"rowVersion\"");
    }

    /// <summary>
    ///     Verifies that business reward-configuration contracts serialize tier and
    ///     mutation payload fields with expected camelCase names.
    /// </summary>
    [Fact]
    public void BusinessRewardConfigurationContracts_Should_Serialize_WithExpectedPropertyNames()
    {
        // Arrange
        var config = new BusinessRewardConfigurationResponse
        {
            LoyaltyProgramId = Guid.Parse("abababab-abab-abab-abab-abababababab"),
            ProgramName = "Cafe Rewards",
            IsProgramActive = true,
            RewardTiers =
            [
                new BusinessRewardTierConfigItem
                {
                    RewardTierId = Guid.Parse("cdcdcdcd-cdcd-cdcd-cdcd-cdcdcdcdcdcd"),
                    PointsRequired = 120,
                    RewardType = "FreeDrink",
                    RewardValue = null,
                    Description = "One small coffee",
                    AllowSelfRedemption = true,
                    RowVersion = [9, 9, 9]
                }
            ]
        };

        var mutation = new BusinessRewardTierMutationResponse
        {
            RewardTierId = Guid.Parse("efefefef-efef-efef-efef-efefefefefef"),
            Success = true
        };

        // Act
        var configJson = JsonSerializer.Serialize(config, JsonOptions);
        var mutationJson = JsonSerializer.Serialize(mutation, JsonOptions);

        // Assert
        configJson.Should().Contain("\"loyaltyProgramId\"");
        configJson.Should().Contain("\"programName\"");
        configJson.Should().Contain("\"isProgramActive\"");
        configJson.Should().Contain("\"rewardTiers\"");
        configJson.Should().Contain("\"rowVersion\"");

        mutationJson.Should().Contain("\"rewardTierId\"");
        mutationJson.Should().Contain("\"success\"");
    }


    /// <summary>
    ///     Verifies that campaign listing response deserializes nested items while tolerating
    ///     unknown future fields used for non-breaking API expansion.
    /// </summary>
    [Fact]
    public void GetBusinessCampaignsResponse_Should_Deserialize_WithNestedItemsAndUnknownFields()
    {
        // Arrange
        const string json = """
            {
              "items": [
                {
                  "id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
                  "businessId": "99999999-8888-7777-6666-555555555555",
                  "name": "Weekend Boost",
                  "title": "Double Points",
                  "subtitle": "Fri-Sun",
                  "body": "Earn 2x points.",
                  "mediaUrl": "https://cdn.example/campaign.jpg",
                  "landingUrl": "https://example.test/rewards",
                  "channels": 3,
                  "startsAtUtc": "2030-08-01T08:00:00Z",
                  "endsAtUtc": "2030-08-03T20:00:00Z",
                  "campaignState": "Active",
                  "futureField": "ignored"
                }
              ],
              "total": 1,
              "unknownTopLevel": true
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<GetBusinessCampaignsResponse>(json, JsonOptions);

        // Assert
        dto.Should().NotBeNull();
        dto!.Total.Should().Be(1);
        dto.Items.Should().HaveCount(1);
        dto.Items[0].Id.Should().Be(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        dto.Items[0].Name.Should().Be("Weekend Boost");
        dto.Items[0].Channels.Should().Be(3);
    }

    /// <summary>
    ///     Verifies that reward-tier delete request serializes concurrency-sensitive fields
    ///     with stable camelCase names consumed by business management screens.
    /// </summary>
    [Fact]
    public void DeleteBusinessRewardTierRequest_Should_Serialize_WithExpectedPropertyNames()
    {
        // Arrange
        var dto = new DeleteBusinessRewardTierRequest
        {
            RewardTierId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            RowVersion = [4, 3, 2, 1]
        };

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        // Assert
        json.Should().Contain("\"rewardTierId\"");
        json.Should().Contain("\"rowVersion\"");
    }


    /// <summary>
    ///     Verifies that campaign listing response serializes collection wrapper and
    ///     nested campaign fields with stable camelCase names expected by clients.
    /// </summary>
    [Fact]
    public void GetBusinessCampaignsResponse_Should_Serialize_WithExpectedPropertyNames()
    {
        // Arrange
        var dto = new GetBusinessCampaignsResponse
        {
            Items =
            [
                new BusinessCampaignItem
                {
                    Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    BusinessId = Guid.Parse("99999999-8888-7777-6666-555555555555"),
                    Name = "Weekend Boost",
                    Title = "Double Points",
                    Channels = 3,
                    CampaignState = "Active",
                    RowVersion = [1, 2, 3, 4]
                }
            ],
            Total = 1
        };

        // Act
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        // Assert
        json.Should().Contain("\"items\"");
        json.Should().Contain("\"total\"");
        json.Should().Contain("\"businessId\"");
        json.Should().Contain("\"campaignState\"");
        json.Should().Contain("\"rowVersion\"");
    }

    /// <summary>
    ///     Verifies that reward-tier delete request deserializes identifier and
    ///     concurrency token from a standard camelCase payload.
    /// </summary>
    [Fact]
    public void DeleteBusinessRewardTierRequest_Should_Deserialize_WithExpectedPropertyNames()
    {
        // Arrange
        const string json = """
            {
              "rewardTierId": "11111111-2222-3333-4444-555555555555",
              "rowVersion": "BAQDAg==",
              "futureField": "ignored"
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<DeleteBusinessRewardTierRequest>(json, JsonOptions);

        // Assert
        dto.Should().NotBeNull();
        dto!.RewardTierId.Should().Be(Guid.Parse("11111111-2222-3333-4444-555555555555"));
        dto.RowVersion.Should().Equal(4, 4, 3, 2);
    }


    /// <summary>
    ///     Verifies that campaign create request deserializes mutable payload fields
    ///     from camelCase JSON for client/server compatibility.
    /// </summary>
    [Fact]
    public void CreateBusinessCampaignRequest_Should_Deserialize_WithExpectedPropertyNames()
    {
        // Arrange
        const string json = """
            {
              "name": "Weekend Boost",
              "title": "Double Points",
              "subtitle": "Fri-Sun",
              "body": "Earn 2x points.",
              "mediaUrl": "https://cdn.example/campaign.jpg",
              "landingUrl": "https://example.test/rewards",
              "channels": 3,
              "startsAtUtc": "2030-08-01T08:00:00Z",
              "endsAtUtc": "2030-08-03T20:00:00Z",
              "targetingJson": "{\"tier\":\"gold\"}",
              "payloadJson": "{\"kind\":\"boost\"}",
              "futureField": "ignored"
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<CreateBusinessCampaignRequest>(json, JsonOptions);

        // Assert
        dto.Should().NotBeNull();
        dto!.Name.Should().Be("Weekend Boost");
        dto.Title.Should().Be("Double Points");
        dto.Channels.Should().Be(3);
        dto.StartsAtUtc.Should().Be(new DateTime(2030, 8, 1, 8, 0, 0, DateTimeKind.Utc));
        dto.EndsAtUtc.Should().Be(new DateTime(2030, 8, 3, 20, 0, 0, DateTimeKind.Utc));
        dto.TargetingJson.Should().Contain("tier");
        dto.PayloadJson.Should().Contain("kind");
    }

    /// <summary>
    ///     Verifies that campaign update request deserializes identity/concurrency fields
    ///     and mutable payload content from camelCase JSON.
    /// </summary>
    [Fact]
    public void UpdateBusinessCampaignRequest_Should_Deserialize_WithExpectedPropertyNames()
    {
        // Arrange
        const string json = """
            {
              "id": "12121212-3434-5656-7878-909090909090",
              "name": "Weekend Boost v2",
              "title": "Triple Points",
              "channels": 1,
              "startsAtUtc": "2030-08-10T09:00:00Z",
              "endsAtUtc": "2030-08-12T19:30:00Z",
              "targetingJson": "{\"tier\":\"platinum\"}",
              "payloadJson": "{\"kind\":\"boost-v2\"}",
              "rowVersion": "AQID",
              "futureField": "ignored"
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<UpdateBusinessCampaignRequest>(json, JsonOptions);

        // Assert
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(Guid.Parse("12121212-3434-5656-7878-909090909090"));
        dto.Name.Should().Be("Weekend Boost v2");
        dto.Title.Should().Be("Triple Points");
        dto.Channels.Should().Be(1);
        dto.StartsAtUtc.Should().Be(new DateTime(2030, 8, 10, 9, 0, 0, DateTimeKind.Utc));
        dto.EndsAtUtc.Should().Be(new DateTime(2030, 8, 12, 19, 30, 0, DateTimeKind.Utc));
        dto.RowVersion.Should().Equal(1, 2, 3);
    }


    /// <summary>
    ///     Verifies that business reward configuration response deserializes reward-tier
    ///     entries and concurrency tokens from camelCase payloads.
    /// </summary>
    [Fact]
    public void BusinessRewardConfigurationResponse_Should_Deserialize_WithExpectedPropertyNames()
    {
        // Arrange
        const string json = """
            {
              "loyaltyProgramId": "abababab-abab-abab-abab-abababababab",
              "programName": "Cafe Rewards",
              "isProgramActive": true,
              "rewardTiers": [
                {
                  "rewardTierId": "cdcdcdcd-cdcd-cdcd-cdcd-cdcdcdcdcdcd",
                  "pointsRequired": 120,
                  "rewardType": "FreeDrink",
                  "rewardValue": "small",
                  "description": "One small coffee",
                  "allowSelfRedemption": true,
                  "rowVersion": "CQkJ"
                }
              ],
              "futureField": "ignored"
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<BusinessRewardConfigurationResponse>(json, JsonOptions);

        // Assert
        dto.Should().NotBeNull();
        dto!.LoyaltyProgramId.Should().Be(Guid.Parse("abababab-abab-abab-abab-abababababab"));
        dto.ProgramName.Should().Be("Cafe Rewards");
        dto.IsProgramActive.Should().BeTrue();
        dto.RewardTiers.Should().HaveCount(1);
        dto.RewardTiers[0].RewardTierId.Should().Be(Guid.Parse("cdcdcdcd-cdcd-cdcd-cdcd-cdcdcdcdcdcd"));
        dto.RewardTiers[0].RowVersion.Should().Equal(9, 9, 9);
    }

    /// <summary>
    ///     Verifies that reward-tier mutation response deserializes identity and status
    ///     fields from camelCase payload names.
    /// </summary>
    [Fact]
    public void BusinessRewardTierMutationResponse_Should_Deserialize_WithExpectedPropertyNames()
    {
        // Arrange
        const string json = """
            {
              "rewardTierId": "efefefef-efef-efef-efef-efefefefefef",
              "success": true,
              "futureField": "ignored"
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<BusinessRewardTierMutationResponse>(json, JsonOptions);

        // Assert
        dto.Should().NotBeNull();
        dto!.RewardTierId.Should().Be(Guid.Parse("efefefef-efef-efef-efef-efefefefefef"));
        dto.Success.Should().BeTrue();
    }


    /// <summary>
    ///     Verifies that campaign listing deserialization preserves business identifier
    ///     and rowVersion concurrency token for nested campaign items.
    /// </summary>
    [Fact]
    public void GetBusinessCampaignsResponse_Should_Deserialize_BusinessIdAndRowVersionFields()
    {
        // Arrange
        const string json = """
            {
              "items": [
                {
                  "id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
                  "businessId": "99999999-8888-7777-6666-555555555555",
                  "name": "Weekend Boost",
                  "title": "Double Points",
                  "channels": 3,
                  "campaignState": "Active",
                  "rowVersion": "AQIDBA=="
                }
              ],
              "total": 1
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<GetBusinessCampaignsResponse>(json, JsonOptions);

        // Assert
        dto.Should().NotBeNull();
        dto!.Items.Should().HaveCount(1);
        dto.Items[0].BusinessId.Should().Be(Guid.Parse("99999999-8888-7777-6666-555555555555"));
        dto.Items[0].CampaignState.Should().Be("Active");
        dto.Items[0].RowVersion.Should().Equal(1, 2, 3, 4);
    }

}
