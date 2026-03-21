using Darwin.Contracts.Businesses;
using Darwin.Contracts.Common;
using Darwin.Contracts.Identity;
using Darwin.Contracts.Loyalty;
using FluentAssertions;
using System.Text.Json;

namespace Darwin.Contracts.Tests.Serialization;

/// <summary>
///     Provides contract-level serialization smoke tests for DTOs that are
///     directly consumed by mobile applications.
/// </summary>
public sealed class ContractsSerializationSmokeTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     Verifies that <see cref="TokenResponse"/> serializes and deserializes
    ///     without data loss for authentication-critical fields.
    /// </summary>
    [Fact]
    public void TokenResponse_Should_RoundTripSerialization()
    {
        // Arrange
        var model = new TokenResponse
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
            RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            UserId = Guid.NewGuid(),
            Email = "user@example.test",
            Scopes = ["member.read", "member.write"]
        };

        // Act
        var json = JsonSerializer.Serialize(model, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<TokenResponse>(json, JsonOptions);

        // Assert
        roundTrip.Should().NotBeNull();
        roundTrip!.AccessToken.Should().Be("access-token");
        roundTrip.RefreshToken.Should().Be("refresh-token");
        roundTrip.UserId.Should().Be(model.UserId);
        roundTrip.Email.Should().Be("user@example.test");
        roundTrip.Scopes.Should().Equal(["member.read", "member.write"]);
    }

    /// <summary>
    ///     Verifies map discovery request and business summary contracts keep
    ///     viewport, paging, and location/rating fields stable across JSON boundaries.
    /// </summary>
    [Fact]
    public void BusinessDiscoveryContracts_Should_RoundTripCoordinatesAndRatingFields()
    {
        // Arrange
        var request = new BusinessMapDiscoveryRequest
        {
            Bounds = new GeoBoundsModel
            {
                NorthLat = 52.60,
                SouthLat = 52.50,
                EastLon = 13.50,
                WestLon = 13.30
            },
            Page = 2,
            PageSize = 100,
            Query = "coffee",
            Category = "Cafe",
            CountryCode = "DE"
        };

        var summary = new BusinessSummary
        {
            Id = Guid.NewGuid(),
            Name = "Darwin Cafe",
            Category = "Cafe",
            Location = new GeoCoordinateModel { Latitude = 52.52, Longitude = 13.40, AltitudeMeters = 34.2 },
            Rating = 4.8,
            RatingCount = 25,
            DistanceMeters = 740
        };

        // Act
        var requestJson = JsonSerializer.Serialize(request, JsonOptions);
        var summaryJson = JsonSerializer.Serialize(summary, JsonOptions);

        var requestRoundTrip = JsonSerializer.Deserialize<BusinessMapDiscoveryRequest>(requestJson, JsonOptions);
        var summaryRoundTrip = JsonSerializer.Deserialize<BusinessSummary>(summaryJson, JsonOptions);

        // Assert
        requestRoundTrip.Should().NotBeNull();
        requestRoundTrip!.Bounds.Should().NotBeNull();
        requestRoundTrip.Bounds!.NorthLat.Should().Be(52.60);
        requestRoundTrip.Bounds.WestLon.Should().Be(13.30);
        requestRoundTrip.Page.Should().Be(2);
        requestRoundTrip.PageSize.Should().Be(100);
        requestRoundTrip.CountryCode.Should().Be("DE");

        summaryRoundTrip.Should().NotBeNull();
        summaryRoundTrip!.Location.Should().NotBeNull();
        summaryRoundTrip.Location!.Longitude.Should().Be(13.40);
        summaryRoundTrip.Rating.Should().Be(4.8);
        summaryRoundTrip.DistanceMeters.Should().Be(740);
    }

    /// <summary>
    ///     Verifies loyalty timeline page keeps paging cursors and entry payloads
    ///     stable across serialization.
    /// </summary>
    [Fact]
    public void LoyaltyTimelinePageResponse_Should_RoundTripWithItemsAndCursor()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var model = new GetMyLoyaltyTimelinePageResponse
        {
            Items =
            [
                new LoyaltyTimelineEntry
                {
                    Id = entryId,
                    Kind = LoyaltyTimelineEntryKind.PointsTransaction,
                    LoyaltyAccountId = Guid.NewGuid(),
                    BusinessId = Guid.NewGuid(),
                    OccurredAtUtc = DateTime.UtcNow,
                    PointsDelta = 15,
                    PointsSpent = null,
                    RewardTierId = null,
                    Reference = "txn-101",
                    Note = "Accrual"
                }
            ],
            NextBeforeAtUtc = DateTime.UtcNow.AddMinutes(-2),
            NextBeforeId = Guid.NewGuid()
        };

        // Act
        var json = JsonSerializer.Serialize(model, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<GetMyLoyaltyTimelinePageResponse>(json, JsonOptions);

        // Assert
        roundTrip.Should().NotBeNull();
        roundTrip!.Items.Should().HaveCount(1);
        roundTrip.Items[0].Id.Should().Be(entryId);
        roundTrip.NextBeforeAtUtc.Should().NotBeNull();
        roundTrip.NextBeforeId.Should().NotBeNull();
    }

    /// <summary>
    ///     Verifies that the member profile contract round-trips optimistic-concurrency
    ///     payload fields (<c>Id</c> + <c>RowVersion</c>) required by mobile update flows.
    /// </summary>
    [Fact]
    public void CustomerProfile_Should_RoundTripWithRowVersionAndIdentityFields()
    {
        // Arrange
        var profile = new Darwin.Contracts.Profile.CustomerProfile
        {
            Id = Guid.NewGuid(),
            Email = "profile@example.test",
            FirstName = "Ada",
            LastName = "Lovelace",
            PhoneE164 = "+491701234567",
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR",
            RowVersion = [1, 2, 3, 4]
        };

        // Act
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<Darwin.Contracts.Profile.CustomerProfile>(json, JsonOptions);

        // Assert
        roundTrip.Should().NotBeNull();
        roundTrip!.Id.Should().Be(profile.Id);
        roundTrip.Email.Should().Be("profile@example.test");
        roundTrip.RowVersion.Should().Equal([1, 2, 3, 4]);
    }

    /// <summary>
    ///     Verifies promotion feed contracts preserve policy diagnostics and campaign-aware
    ///     payload fields used by mobile feed rendering and suppression behavior.
    /// </summary>
    [Fact]
    public void MyPromotionsResponse_Should_RoundTripWithPolicyAndCampaignFields()
    {
        // Arrange
        var response = new MyPromotionsResponse
        {
            Items =
            [
                new PromotionFeedItem
                {
                    BusinessId = Guid.NewGuid(),
                    BusinessName = "Darwin Cafe",
                    Title = "Double points weekend",
                    Description = "Collect 2x points on all orders.",
                    CtaKind = "OpenRewards",
                    Priority = 5,
                    CampaignId = Guid.NewGuid(),
                    CampaignState = PromotionCampaignState.Active,
                    StartsAtUtc = DateTime.UtcNow.AddHours(-1),
                    EndsAtUtc = DateTime.UtcNow.AddDays(1),
                    EligibilityRules =
                    [
                        new PromotionEligibilityRule
                        {
                            AudienceKind = PromotionAudienceKind.JoinedMembers,
                            Note = "Available to joined loyalty members."
                        }
                    ]
                }
            ],
            AppliedPolicy = new PromotionFeedPolicy
            {
                EnableDeduplication = true,
                MaxCards = 6,
                SuppressionWindowMinutes = 480,
                FrequencyWindowMinutes = 120
            },
            Diagnostics = new PromotionFeedDiagnostics
            {
                InitialCandidates = 4,
                SuppressedByFrequency = 1,
                Deduplicated = 2,
                TrimmedByCap = 0,
                FinalCount = 1
            }
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<MyPromotionsResponse>(json, JsonOptions);

        // Assert
        roundTrip.Should().NotBeNull();
        roundTrip!.Items.Should().HaveCount(1);
        roundTrip.Items[0].CampaignState.Should().Be(PromotionCampaignState.Active);
        roundTrip.Items[0].EligibilityRules.Should().HaveCount(1);
        roundTrip.AppliedPolicy.Should().NotBeNull();
        roundTrip.AppliedPolicy!.FrequencyWindowMinutes.Should().Be(120);
        roundTrip.Diagnostics.Should().NotBeNull();
        roundTrip.Diagnostics!.Deduplicated.Should().Be(2);
    }

    /// <summary>
    ///     Verifies that push-device registration contracts keep platform, token, and
    ///     permission metadata stable across serialization boundaries.
    /// </summary>
    [Fact]
    public void RegisterPushDeviceContracts_Should_RoundTripPlatformAndPermissionFields()
    {
        // Arrange
        var request = new Darwin.Contracts.Notifications.RegisterPushDeviceRequest
        {
            Platform = Darwin.Contracts.Notifications.MobileDevicePlatform.Android,
            DeviceId = "android-device-1",
            PushToken = "fcm-token",
            AppVersion = "1.2.3",
            DeviceModel = "Pixel 9",
            NotificationsEnabled = true
        };

        var response = new Darwin.Contracts.Notifications.RegisterPushDeviceResponse
        {
            DeviceId = "android-device-1",
            RegisteredAtUtc = DateTime.UtcNow
        };

        // Act
        var requestJson = JsonSerializer.Serialize(request, JsonOptions);
        var responseJson = JsonSerializer.Serialize(response, JsonOptions);

        var requestRoundTrip = JsonSerializer.Deserialize<Darwin.Contracts.Notifications.RegisterPushDeviceRequest>(requestJson, JsonOptions);
        var responseRoundTrip = JsonSerializer.Deserialize<Darwin.Contracts.Notifications.RegisterPushDeviceResponse>(responseJson, JsonOptions);

        // Assert
        requestRoundTrip.Should().NotBeNull();
        requestRoundTrip!.Platform.Should().Be(Darwin.Contracts.Notifications.MobileDevicePlatform.Android);
        requestRoundTrip.NotificationsEnabled.Should().BeTrue();
        requestRoundTrip.DeviceModel.Should().Be("Pixel 9");

        responseRoundTrip.Should().NotBeNull();
        responseRoundTrip!.DeviceId.Should().Be("android-device-1");
    }

    /// <summary>
    ///     Verifies that scan-session contracts round-trip fields needed by both
    ///     Consumer and Business apps during prepare/process/confirm flow.
    /// </summary>
    [Fact]
    public void ScanSessionContracts_Should_RoundTripPrepareAndProcessPayloads()
    {
        // Arrange
        var prepare = new PrepareScanSessionResponse
        {
            ScanSessionToken = "scan-token-123",
            Mode = LoyaltyScanMode.Redemption,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(4),
            CurrentPointsBalance = 140,
            SelectedRewards =
            [
                new LoyaltyRewardSummary
                {
                    LoyaltyRewardTierId = Guid.NewGuid(),
                    BusinessId = Guid.NewGuid(),
                    Name = "Free Coffee",
                    RequiredPoints = 120,
                    Description = "One small coffee.",
                    IsActive = true,
                    IsSelectable = true
                }
            ]
        };

        var process = new ProcessScanSessionForBusinessResponse
        {
            Mode = LoyaltyScanMode.Redemption,
            BusinessId = Guid.NewGuid(),
            AllowedActions = LoyaltyScanAllowedActions.CanConfirmRedemption,
            AccountSummary = new BusinessLoyaltyAccountSummary
            {
                LoyaltyAccountId = Guid.NewGuid(),
                PointsBalance = 140,
                CustomerDisplayName = "Customer #1001"
            },
            SelectedRewards =
            [
                new LoyaltyRewardSummary
                {
                    LoyaltyRewardTierId = Guid.NewGuid(),
                    BusinessId = Guid.NewGuid(),
                    Name = "Free Coffee",
                    RequiredPoints = 120,
                    Description = "One small coffee.",
                    IsActive = true,
                    IsSelectable = true
                }
            ]
        };

        // Act
        var prepareJson = JsonSerializer.Serialize(prepare, JsonOptions);
        var processJson = JsonSerializer.Serialize(process, JsonOptions);

        var prepareRoundTrip = JsonSerializer.Deserialize<PrepareScanSessionResponse>(prepareJson, JsonOptions);
        var processRoundTrip = JsonSerializer.Deserialize<ProcessScanSessionForBusinessResponse>(processJson, JsonOptions);

        // Assert
        prepareRoundTrip.Should().NotBeNull();
        prepareRoundTrip!.ScanSessionToken.Should().Be("scan-token-123");
        prepareRoundTrip.Mode.Should().Be(LoyaltyScanMode.Redemption);
        prepareRoundTrip.SelectedRewards.Should().HaveCount(1);

        processRoundTrip.Should().NotBeNull();
        processRoundTrip!.AllowedActions.Should().Be(LoyaltyScanAllowedActions.CanConfirmRedemption);
        processRoundTrip.AccountSummary.Should().NotBeNull();
        processRoundTrip.SelectedRewards.Should().HaveCount(1);
    }

    /// <summary>
    ///     Verifies business-detail contracts preserve nested loyalty and account
    ///     snapshots needed by mobile business-detail and rewards screens.
    /// </summary>
    [Fact]
    public void BusinessDetailWithMyAccount_Should_RoundTripNestedLoyaltyPayload()
    {
        // Arrange
        var model = new BusinessDetailWithMyAccount
        {
            Business = new BusinessDetail
            {
                Id = Guid.NewGuid(),
                Name = "Darwin Market",
                Category = "Grocery",
                ShortDescription = "Healthy food",
                Description = "Healthy food and daily essentials.",
                City = "Berlin",
                Coordinate = new GeoCoordinateModel { Latitude = 52.5, Longitude = 13.4 },
                DefaultCurrency = "EUR",
                DefaultCulture = "de-DE"
            },
            HasAccount = true,
            MyAccount = new LoyaltyAccountSummary
            {
                LoyaltyAccountId = Guid.NewGuid(),
                BusinessId = Guid.NewGuid(),
                BusinessName = "Darwin Market",
                PointsBalance = 75,
                LifetimePoints = 300,
                LastAccrualAtUtc = DateTime.UtcNow.AddDays(-2),
                NextRewardTitle = "Free delivery voucher",
                Status = "Active"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(model, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<BusinessDetailWithMyAccount>(json, JsonOptions);

        // Assert
        roundTrip.Should().NotBeNull();
        roundTrip!.Business.Should().NotBeNull();
        roundTrip.Business!.Name.Should().Be("Darwin Market");
        roundTrip.MyAccount.Should().NotBeNull();
        roundTrip.MyAccount!.PointsBalance.Should().Be(75);
        roundTrip.HasAccount.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that identity request contracts round-trip mobile-auth payloads
    ///     used in register, login, refresh and password-reset flows.
    /// </summary>
    [Fact]
    public void IdentityRequestContracts_Should_RoundTripCoreMobileAuthPayloads()
    {
        // Arrange
        var register = new RegisterRequest
        {
            FirstName = "Test",
            LastName = "User",
            Email = "register@example.test",
            Password = "P@ssw0rd!Aa1"
        };

        var login = new PasswordLoginRequest
        {
            Email = "register@example.test",
            Password = "P@ssw0rd!Aa1",
            DeviceId = "device-1"
        };

        var refresh = new RefreshTokenRequest
        {
            RefreshToken = "refresh-token",
            DeviceId = "device-1"
        };

        var requestReset = new RequestPasswordResetRequest
        {
            Email = "register@example.test"
        };

        var reset = new ResetPasswordRequest
        {
            Email = "register@example.test",
            Token = "reset-token",
            NewPassword = "N3wP@ssw0rd!Bb2"
        };

        // Act
        var registerRoundTrip = JsonSerializer.Deserialize<RegisterRequest>(JsonSerializer.Serialize(register, JsonOptions), JsonOptions);
        var loginRoundTrip = JsonSerializer.Deserialize<PasswordLoginRequest>(JsonSerializer.Serialize(login, JsonOptions), JsonOptions);
        var refreshRoundTrip = JsonSerializer.Deserialize<RefreshTokenRequest>(JsonSerializer.Serialize(refresh, JsonOptions), JsonOptions);
        var requestResetRoundTrip = JsonSerializer.Deserialize<RequestPasswordResetRequest>(JsonSerializer.Serialize(requestReset, JsonOptions), JsonOptions);
        var resetRoundTrip = JsonSerializer.Deserialize<ResetPasswordRequest>(JsonSerializer.Serialize(reset, JsonOptions), JsonOptions);

        // Assert
        registerRoundTrip.Should().NotBeNull();
        registerRoundTrip!.Email.Should().Be("register@example.test");
        loginRoundTrip.Should().NotBeNull();
        loginRoundTrip!.DeviceId.Should().Be("device-1");
        refreshRoundTrip.Should().NotBeNull();
        refreshRoundTrip!.RefreshToken.Should().Be("refresh-token");
        requestResetRoundTrip.Should().NotBeNull();
        requestResetRoundTrip!.Email.Should().Be("register@example.test");
        resetRoundTrip.Should().NotBeNull();
        resetRoundTrip!.Token.Should().Be("reset-token");
    }

    /// <summary>
    ///     Verifies business campaign management contracts round-trip mutable fields,
    ///     concurrency tokens, and paging payload used by mobile business workflows.
    /// </summary>
    [Fact]
    public void BusinessCampaignContracts_Should_RoundTripMutationAndListPayloads()
    {
        // Arrange
        var createRequest = new CreateBusinessCampaignRequest
        {
            Name = "Weekend Boost",
            Title = "Double Points Weekend",
            Subtitle = "Fri-Sun",
            Body = "Collect double points this weekend.",
            MediaUrl = "https://cdn.example.test/campaign.png",
            LandingUrl = "https://example.test/rewards",
            Channels = 3,
            StartsAtUtc = DateTime.UtcNow,
            EndsAtUtc = DateTime.UtcNow.AddDays(2),
            TargetingJson = "{\"joined\":true}",
            PayloadJson = "{\"kind\":\"boost\"}"
        };

        var updateRequest = new UpdateBusinessCampaignRequest
        {
            Id = Guid.NewGuid(),
            Name = "Weekend Boost v2",
            Title = "Triple Points Weekend",
            Subtitle = "Limited",
            Body = "Collect triple points today.",
            MediaUrl = null,
            LandingUrl = "https://example.test/rewards",
            Channels = 1,
            StartsAtUtc = DateTime.UtcNow,
            EndsAtUtc = DateTime.UtcNow.AddDays(1),
            TargetingJson = "{\"tier\":\"gold\"}",
            PayloadJson = "{\"kind\":\"boost-v2\"}",
            RowVersion = [1, 2, 3]
        };

        var listResponse = new GetBusinessCampaignsResponse
        {
            Items =
            [
                new BusinessCampaignItem
                {
                    Id = Guid.NewGuid(),
                    BusinessId = Guid.NewGuid(),
                    Name = "Weekend Boost",
                    Title = "Double Points Weekend",
                    Subtitle = "Fri-Sun",
                    Body = "Collect double points this weekend.",
                    MediaUrl = "https://cdn.example.test/campaign.png",
                    LandingUrl = "https://example.test/rewards",
                    Channels = 3,
                    StartsAtUtc = DateTime.UtcNow,
                    EndsAtUtc = DateTime.UtcNow.AddDays(2),
                    IsActive = true,
                    CampaignState = PromotionCampaignState.Active,
                    TargetingJson = "{\"joined\":true}",
                    PayloadJson = "{\"kind\":\"boost\"}",
                    RowVersion = [4, 5, 6]
                }
            ],
            Total = 1
        };

        var mutationResponse = new BusinessCampaignMutationResponse
        {
            CampaignId = Guid.NewGuid()
        };

        var activationRequest = new SetCampaignActivationRequest
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            RowVersion = [7, 8]
        };

        // Act
        var createRoundTrip = JsonSerializer.Deserialize<CreateBusinessCampaignRequest>(JsonSerializer.Serialize(createRequest, JsonOptions), JsonOptions);
        var updateRoundTrip = JsonSerializer.Deserialize<UpdateBusinessCampaignRequest>(JsonSerializer.Serialize(updateRequest, JsonOptions), JsonOptions);
        var listRoundTrip = JsonSerializer.Deserialize<GetBusinessCampaignsResponse>(JsonSerializer.Serialize(listResponse, JsonOptions), JsonOptions);
        var mutationRoundTrip = JsonSerializer.Deserialize<BusinessCampaignMutationResponse>(JsonSerializer.Serialize(mutationResponse, JsonOptions), JsonOptions);
        var activationRoundTrip = JsonSerializer.Deserialize<SetCampaignActivationRequest>(JsonSerializer.Serialize(activationRequest, JsonOptions), JsonOptions);

        // Assert
        createRoundTrip.Should().NotBeNull();
        createRoundTrip!.Name.Should().Be("Weekend Boost");
        createRoundTrip.TargetingJson.Should().Contain("joined");

        updateRoundTrip.Should().NotBeNull();
        updateRoundTrip!.RowVersion.Should().Equal([1, 2, 3]);

        listRoundTrip.Should().NotBeNull();
        listRoundTrip!.Items.Should().HaveCount(1);
        listRoundTrip.Items[0].CampaignState.Should().Be(PromotionCampaignState.Active);
        listRoundTrip.Total.Should().Be(1);

        mutationRoundTrip.Should().NotBeNull();
        mutationRoundTrip!.CampaignId.Should().NotBe(Guid.Empty);

        activationRoundTrip.Should().NotBeNull();
        activationRoundTrip!.IsActive.Should().BeTrue();
        activationRoundTrip.RowVersion.Should().Equal([7, 8]);
    }

    /// <summary>
    ///     Verifies business reward-configuration contracts round-trip reward tiers,
    ///     mutation responses, and RowVersion fields for optimistic-concurrency updates.
    /// </summary>
    [Fact]
    public void BusinessRewardConfigurationContracts_Should_RoundTripWithRewardTierConcurrencyPayloads()
    {
        // Arrange
        var configResponse = new BusinessRewardConfigurationResponse
        {
            LoyaltyProgramId = Guid.NewGuid(),
            ProgramName = "Cafe Rewards",
            IsProgramActive = true,
            RewardTiers =
            [
                new BusinessRewardTierConfigItem
                {
                    RewardTierId = Guid.NewGuid(),
                    PointsRequired = 120,
                    RewardType = "FreeDrink",
                    RewardValue = null,
                    Description = "One small coffee",
                    AllowSelfRedemption = true,
                    RowVersion = [3, 2, 1]
                }
            ]
        };

        var mutationResponse = new BusinessRewardTierMutationResponse
        {
            RewardTierId = Guid.NewGuid(),
            Success = true
        };

        // Act
        var configRoundTrip = JsonSerializer.Deserialize<BusinessRewardConfigurationResponse>(JsonSerializer.Serialize(configResponse, JsonOptions), JsonOptions);
        var mutationRoundTrip = JsonSerializer.Deserialize<BusinessRewardTierMutationResponse>(JsonSerializer.Serialize(mutationResponse, JsonOptions), JsonOptions);

        // Assert
        configRoundTrip.Should().NotBeNull();
        configRoundTrip!.IsProgramActive.Should().BeTrue();
        configRoundTrip.RewardTiers.Should().HaveCount(1);
        configRoundTrip.RewardTiers[0].RowVersion.Should().Equal([3, 2, 1]);

        mutationRoundTrip.Should().NotBeNull();
        mutationRoundTrip!.Success.Should().BeTrue();
        mutationRoundTrip.RewardTierId.Should().NotBe(Guid.Empty);
    }

}
