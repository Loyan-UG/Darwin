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
public sealed class ContractSerializationCompatibilityLoyaltyAndBusinessTests : ContractSerializationCompatibilityTestBase
{
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
            AllowedActions = default,
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
    ///     Verifies that loyalty overview contracts serialize aggregate counters and nested
    ///     account summaries with stable camelCase names.
    /// </summary>
    [Fact]
    public void MyLoyaltyOverviewResponse_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new MyLoyaltyOverviewResponse
        {
            TotalAccounts = 2,
            ActiveAccounts = 1,
            TotalPointsBalance = 420,
            TotalLifetimePoints = 1250,
            LastAccrualAtUtc = new DateTime(2030, 1, 3, 8, 0, 0, DateTimeKind.Utc),
            Accounts =
            [
                new LoyaltyAccountSummary
                {
                    LoyaltyAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    BusinessId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    BusinessName = "Cafe Aurora",
                    PointsBalance = 300,
                    LifetimePoints = 1000,
                    Status = "Active",
                    NextRewardTitle = "Free Cake",
                    NextRewardRequiredPoints = 500,
                    PointsToNextReward = 200,
                    NextRewardProgressPercent = 60m
                }
            ]
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"totalAccounts\"");
        json.Should().Contain("\"activeAccounts\"");
        json.Should().Contain("\"totalPointsBalance\"");
        json.Should().Contain("\"totalLifetimePoints\"");
        json.Should().Contain("\"lastAccrualAtUtc\"");
        json.Should().Contain("\"accounts\"");
        json.Should().Contain("\"nextRewardTitle\"");
        json.Should().Contain("\"nextRewardRequiredPoints\"");
        json.Should().Contain("\"pointsToNextReward\"");
        json.Should().Contain("\"nextRewardProgressPercent\"");
    }

/// <summary>
    ///     Verifies that business-scoped loyalty dashboard contracts serialize nested account,
    ///     reward, and transaction sections with stable camelCase names.
    /// </summary>
    [Fact]
    public void MyLoyaltyBusinessDashboard_Should_Serialize_WithExpectedPropertyNames()
    {
        var dto = new MyLoyaltyBusinessDashboard
        {
            Account = new LoyaltyAccountSummary
            {
                LoyaltyAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                BusinessId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                BusinessName = "Cafe Aurora",
                PointsBalance = 300,
                LifetimePoints = 1000,
                Status = "Active",
                NextRewardTitle = "Free Espresso",
                NextRewardRequiredPoints = 250,
                PointsToNextReward = 70,
                NextRewardProgressPercent = 72m
            },
            AvailableRewardsCount = 4,
            RedeemableRewardsCount = 2,
            PointsToNextReward = 70,
            NextRewardRequiredPoints = 250,
            NextRewardProgressPercent = 72m,
            ExpiryTrackingEnabled = false,
            PointsExpiringSoon = 0,
            NextReward = new LoyaltyRewardSummary
            {
                LoyaltyRewardTierId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                BusinessId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Name = "Free Espresso",
                RequiredPoints = 350,
                IsActive = true,
                IsSelectable = false
            },
            RecentTransactions =
            [
                new PointsTransaction
                {
                    OccurredAtUtc = new DateTime(2030, 1, 2, 7, 0, 0, DateTimeKind.Utc),
                    Type = "Accrual",
                    Delta = 50,
                    Reference = "ORD-1001"
                }
            ]
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"account\"");
        json.Should().Contain("\"availableRewardsCount\"");
        json.Should().Contain("\"redeemableRewardsCount\"");
        json.Should().Contain("\"nextReward\"");
        json.Should().Contain("\"recentTransactions\"");
        json.Should().Contain("\"pointsToNextReward\"");
        json.Should().Contain("\"nextRewardRequiredPoints\"");
        json.Should().Contain("\"nextRewardProgressPercent\"");
        json.Should().Contain("\"expiryTrackingEnabled\"");
        json.Should().Contain("\"pointsExpiringSoon\"");
        json.Should().Contain("\"nextPointsExpiryAtUtc\"");
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
        createJson.Should().Contain("\"subtitle\"");
        createJson.Should().Contain("\"body\"");
        createJson.Should().Contain("\"mediaUrl\"");
        createJson.Should().Contain("\"landingUrl\"");
        createJson.Should().Contain("\"channels\"");
        createJson.Should().Contain("\"targetingJson\"");
        createJson.Should().Contain("\"payloadJson\"");
        createJson.Should().Contain("\"startsAtUtc\"");
        createJson.Should().Contain("\"endsAtUtc\"");

        updateJson.Should().Contain("\"id\"");
        updateJson.Should().Contain("\"subtitle\"");
        updateJson.Should().Contain("\"body\"");
        updateJson.Should().Contain("\"mediaUrl\"");
        updateJson.Should().Contain("\"landingUrl\"");
        updateJson.Should().Contain("\"channels\"");
        updateJson.Should().Contain("\"startsAtUtc\"");
        updateJson.Should().Contain("\"endsAtUtc\"");
        updateJson.Should().Contain("\"targetingJson\"");
        updateJson.Should().Contain("\"payloadJson\"");
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
                  "targetingJson": "{\"tier\":\"gold\"}",
                  "payloadJson": "{\"kind\":\"boost\"}",
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
        dto.Items[0].Subtitle.Should().Be("Fri-Sun");
        dto.Items[0].Body.Should().Be("Earn 2x points.");
        dto.Items[0].MediaUrl.Should().Be("https://cdn.example/campaign.jpg");
        dto.Items[0].LandingUrl.Should().Be("https://example.test/rewards");
        dto.Items[0].TargetingJson.Should().Be("{\"tier\":\"gold\"}");
        dto.Items[0].PayloadJson.Should().Be("{\"kind\":\"boost\"}");
        dto.Items[0].CampaignState.Should().Be("Active");
        dto.Items[0].StartsAtUtc.Should().Be(new DateTime(2030, 8, 1, 8, 0, 0, DateTimeKind.Utc));
        dto.Items[0].EndsAtUtc.Should().Be(new DateTime(2030, 8, 3, 20, 0, 0, DateTimeKind.Utc));
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

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("rewardTierId").GetString().Should().Be("11111111-2222-3333-4444-555555555555");
        doc.RootElement.GetProperty("rowVersion").GetString().Should().Be("BAMCAQ==");
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
                    Subtitle = "Fri-Sun",
                    Body = "Earn 2x points.",
                    MediaUrl = "https://cdn.example/campaign.jpg",
                    LandingUrl = "https://example.test/rewards",
                    Channels = 3,
                    StartsAtUtc = new DateTime(2030, 8, 1, 8, 0, 0, DateTimeKind.Utc),
                    EndsAtUtc = new DateTime(2030, 8, 3, 20, 0, 0, DateTimeKind.Utc),
                    CampaignState = "Active",
                    TargetingJson = "{\"tier\":\"gold\"}",
                    PayloadJson = "{\"kind\":\"boost\"}",
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
        json.Should().Contain("\"id\"");
        json.Should().Contain("\"businessId\"");
        json.Should().Contain("\"name\"");
        json.Should().Contain("\"title\"");
        json.Should().Contain("\"channels\"");
        json.Should().Contain("\"subtitle\"");
        json.Should().Contain("\"body\"");
        json.Should().Contain("\"mediaUrl\"");
        json.Should().Contain("\"landingUrl\"");
        json.Should().Contain("\"startsAtUtc\"");
        json.Should().Contain("\"endsAtUtc\"");
        json.Should().Contain("\"campaignState\"");
        json.Should().Contain("\"targetingJson\"");
        json.Should().Contain("\"payloadJson\"");
        json.Should().Contain("\"rowVersion\"");

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("total").GetInt32().Should().Be(1);
        var firstItem = doc.RootElement.GetProperty("items")[0];
        firstItem.GetProperty("id").GetString().Should().Be("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        firstItem.GetProperty("rowVersion").GetString().Should().Be("AQIDBA==");
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
              "rowVersion": "BAMCAQ==",
              "futureField": "ignored"
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<DeleteBusinessRewardTierRequest>(json, JsonOptions);

        // Assert
        dto.Should().NotBeNull();
        dto!.RewardTierId.Should().Be(Guid.Parse("11111111-2222-3333-4444-555555555555"));
        dto.RowVersion.Should().Equal(4, 3, 2, 1);
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
        dto.Subtitle.Should().Be("Fri-Sun");
        dto.Body.Should().Be("Earn 2x points.");
        dto.MediaUrl.Should().Be("https://cdn.example/campaign.jpg");
        dto.LandingUrl.Should().Be("https://example.test/rewards");
        dto.Channels.Should().Be(3);
        dto.StartsAtUtc.Should().Be(new DateTime(2030, 8, 1, 8, 0, 0, DateTimeKind.Utc));
        dto.EndsAtUtc.Should().Be(new DateTime(2030, 8, 3, 20, 0, 0, DateTimeKind.Utc));
        dto.TargetingJson.Should().Be("{\"tier\":\"gold\"}");
        dto.PayloadJson.Should().Be("{\"kind\":\"boost\"}");
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
              "subtitle": "Mon-Wed",
              "body": "Earn 3x points.",
              "mediaUrl": "https://cdn.example/campaign-v2.jpg",
              "landingUrl": "https://example.test/rewards/v2",
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
        dto.Subtitle.Should().Be("Mon-Wed");
        dto.Body.Should().Be("Earn 3x points.");
        dto.MediaUrl.Should().Be("https://cdn.example/campaign-v2.jpg");
        dto.LandingUrl.Should().Be("https://example.test/rewards/v2");
        dto.Channels.Should().Be(1);
        dto.StartsAtUtc.Should().Be(new DateTime(2030, 8, 10, 9, 0, 0, DateTimeKind.Utc));
        dto.EndsAtUtc.Should().Be(new DateTime(2030, 8, 12, 19, 30, 0, DateTimeKind.Utc));
        dto.TargetingJson.Should().Be("{\"tier\":\"platinum\"}");
        dto.PayloadJson.Should().Be("{\"kind\":\"boost-v2\"}");
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
                  "rewardValue": 1.5,
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
        dto.RewardTiers[0].PointsRequired.Should().Be(120);
        dto.RewardTiers[0].RewardType.Should().Be("FreeDrink");
        dto.RewardTiers[0].RewardValue.Should().Be(1.5m);
        dto.RewardTiers[0].AllowSelfRedemption.Should().BeTrue();
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

/// <summary>
    ///     Verifies that reward-tier mutation response deserializes negative status
    ///     payloads without defaulting the success flag to true.
    /// </summary>
    [Fact]
    public void BusinessRewardTierMutationResponse_Should_Deserialize_FailureStatus()
    {
        // Arrange
        const string json = """
            {
              "rewardTierId": "efefefef-efef-efef-efef-efefefefefef",
              "success": false
            }
            """;

        // Act
        var dto = JsonSerializer.Deserialize<BusinessRewardTierMutationResponse>(json, JsonOptions);

        // Assert
        dto.Should().NotBeNull();
        dto!.RewardTierId.Should().Be(Guid.Parse("efefefef-efef-efef-efef-efefefefefef"));
        dto.Success.Should().BeFalse();
    }

    /// <summary>
    ///     Verifies that invalid numeric <see cref="PromotionInteractionEventType"/> values are not defined
    ///     in the enum, proving the WebApi contract-boundary guard (<c>Enum.IsDefined</c>) would reject them
    ///     with <c>PromotionInteractionEventTypeInvalid</c> instead of silently defaulting to Impression.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    [InlineData(-1)]
    [InlineData(100)]
    public void PromotionInteractionEventType_Should_RejectInvalidNumericValues(int rawValue)
    {
        var eventType = (PromotionInteractionEventType)rawValue;
        Enum.IsDefined(eventType).Should().BeFalse(
            "the WebApi guard uses Enum.IsDefined to reject out-of-range PromotionInteractionEventType values " +
            "and return PromotionInteractionEventTypeInvalid instead of silently defaulting to Impression");
    }

    /// <summary>
    ///     Verifies that valid <see cref="PromotionInteractionEventType"/> values are defined in the enum,
    ///     ensuring the WebApi contract-boundary guard allows legitimate callers through.
    /// </summary>
    [Theory]
    [InlineData(PromotionInteractionEventType.Impression)]
    [InlineData(PromotionInteractionEventType.Open)]
    [InlineData(PromotionInteractionEventType.Claim)]
    public void PromotionInteractionEventType_Should_AcceptValidValues(PromotionInteractionEventType eventType)
    {
        Enum.IsDefined(eventType).Should().BeTrue(
            "valid PromotionInteractionEventType values must pass the Enum.IsDefined guard in the WebApi controller");
    }
}
