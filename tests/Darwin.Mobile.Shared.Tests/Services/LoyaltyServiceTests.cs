using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Common;
using Darwin.Contracts.Billing;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Shared.Results;
using FluentAssertions;

namespace Darwin.Mobile.Shared.Tests.Services;

/// <summary>
/// Covers canonical loyalty service behavior for both consumer and business loyalty flows.
/// </summary>
public sealed class LoyaltyServiceTests
{
    [Fact]
    public async Task PrepareScanSessionAsync_Should_Fail_WhenBusinessIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.PrepareScanSessionAsync(Guid.Empty, LoyaltyScanMode.Accrual, null, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid business identifier.");
    }

    [Fact]
    public async Task PrepareScanSessionAsync_Should_UseCanonicalRouteAndMapResponse()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (route, request, _) =>
            {
                route.Should().Be("api/v1/member/loyalty/scan/prepare");
                request.Should().BeOfType<PrepareScanSessionRequest>();
                return Task.FromResult<object?>(Result<PrepareScanSessionResponse>.Ok(new PrepareScanSessionResponse
                {
                    ScanSessionToken = "token_123",
                    ExpiresAtUtc = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                    Mode = LoyaltyScanMode.Accrual,
                    CurrentPointsBalance = 10
                }));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);

        var result = await service.PrepareScanSessionAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            LoyaltyScanMode.Accrual,
            null,
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be("token_123");
        result.Value!.Mode.Should().Be(LoyaltyScanMode.Accrual);
        result.Value!.ExpiresAtUtc.Should().Be(new DateTimeOffset(new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public async Task PrepareScanSessionAsync_Should_Fail_WhenTokenIsMissing()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (_, _, _) =>
                Task.FromResult<object?>(Result<PrepareScanSessionResponse>.Ok(new PrepareScanSessionResponse
                {
                    ScanSessionToken = " ",
                    ExpiresAtUtc = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                    Mode = LoyaltyScanMode.Accrual,
                    CurrentPointsBalance = 10
                }))
        };
        var service = CreateService(apiClient, TimeProvider.System);

        var result = await service.PrepareScanSessionAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            LoyaltyScanMode.Accrual,
            null,
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Server did not return a valid QR token.");
    }

    [Fact]
    public async Task PrepareScanSessionAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (_, _, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);

        var result = await service.PrepareScanSessionAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            LoyaltyScanMode.Accrual,
            Array.Empty<Guid>(),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("preparing scan session"));
    }

    [Fact]
    public async Task GetAvailableRewardsAsync_Should_Fail_WhenBusinessIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.GetAvailableRewardsAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid business identifier.");
    }

    [Fact]
    public async Task GetAvailableRewardsAsync_Should_UseCanonicalRoute()
    {
        var businessId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (route, _) =>
            {
                route.Should().Be($"api/v1/member/loyalty/business/{businessId:D}/rewards");
                return Task.FromResult<object?>(Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(
                    new List<LoyaltyRewardSummary>
                    {
                        new()
                        {
                            LoyaltyRewardTierId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                            Name = "Loyalty Bonus",
                            RequiredPoints = 100
                        }
                    }));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetAvailableRewardsAsync(businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().ContainSingle(x => x.LoyaltyRewardTierId == Guid.Parse("11111111-2222-3333-4444-555555555555"));
    }

    [Fact]
    public async Task GetAvailableRewardsAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetAvailableRewardsAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving rewards"));
    }

    [Fact]
    public async Task GetAccountSummaryAsync_Should_Fail_WhenBusinessIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.GetAccountSummaryAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid business identifier.");
    }

    [Fact]
    public async Task GetAccountSummaryAsync_Should_UseCanonicalRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (route, _) =>
                Task.FromResult<object?>(Result<LoyaltyAccountSummary>.Ok(new LoyaltyAccountSummary
                {
                    BusinessId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    BusinessName = "Demo Coffee",
                    PointsBalance = 100
                }))
        };
        var service = CreateService(apiClient, TimeProvider.System);

        var result = await service.GetAccountSummaryAsync(Guid.Parse("11111111-2222-3333-4444-555555555555"), TestContext.Current.CancellationToken);

        apiClient.Route.Should().Be("api/v1/member/loyalty/account/11111111-2222-3333-4444-555555555555");
        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.BusinessName.Should().Be("Demo Coffee");
    }

    [Fact]
    public async Task GetAccountSummaryAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetAccountSummaryAsync(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving account summary"));
    }

    [Fact]
    public async Task JoinLoyaltyAsync_Should_Fail_WhenBusinessIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.JoinLoyaltyAsync(Guid.Empty, null, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid business identifier.");
    }

    [Fact]
    public async Task JoinLoyaltyAsync_Should_UseCanonicalRouteAndPassLocation()
    {
        var businessId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var businessLocationId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (route, request, _) =>
            {
                route.Should().Be($"api/v1/member/loyalty/account/{businessId:D}/join");
                request.Should().BeOfType<JoinLoyaltyRequest>();

                var payload = (JoinLoyaltyRequest)request;
                payload.BusinessLocationId.Should().Be(businessLocationId);

                return Task.FromResult<object?>(Result<LoyaltyAccountSummary>.Ok(new LoyaltyAccountSummary
                {
                    BusinessId = businessId,
                    BusinessName = "Demo Coffee",
                    PointsBalance = 42,
                    LastAccrualAtUtc = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc)
                }));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.JoinLoyaltyAsync(businessId, businessLocationId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.PointsBalance.Should().Be(42);
    }

    [Fact]
    public async Task JoinLoyaltyAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (_, _, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);

        var result = await service.JoinLoyaltyAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            null,
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("joining loyalty"));
    }

    [Fact]
    public async Task ConfirmAccrualAsync_Should_Fail_WhenSessionTokenIsMissing()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.ConfirmAccrualAsync(" ", 12, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Session token is required.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ConfirmAccrualAsync_Should_Fail_WhenPointsAreNotPositive(int points)
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.ConfirmAccrualAsync("token", points, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Points must be greater than zero.");
    }

    [Fact]
    public async Task ConfirmAccrualAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (_, _, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.ConfirmAccrualAsync("token", 12, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("confirming accrual"));
    }

    [Fact]
    public async Task ConfirmAccrualAsync_Should_UseCanonicalRouteAndMapSuccess()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (route, request, _) =>
            {
                route.Should().Be("api/v1/business/loyalty/scan/confirm-accrual");
                request.Should().BeOfType<ConfirmAccrualRequest>();
                return Task.FromResult<object?>(Result<ConfirmAccrualResponse>.Ok(new ConfirmAccrualResponse
                {
                    Success = true,
                    ErrorMessage = null,
                    ErrorCode = null,
                    NewBalance = 120,
                    UpdatedAccount = new LoyaltyAccountSummary
                    {
                        BusinessId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                        BusinessName = "Demo Coffee",
                        PointsBalance = 120
                    }
                }));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.ConfirmAccrualAsync("token", 12, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.PointsBalance.Should().Be(120);
    }

    [Fact]
    public async Task ConfirmRedemptionAsync_Should_Fail_WhenSessionTokenIsMissing()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.ConfirmRedemptionAsync(" ", TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Session token is required.");
    }

    [Fact]
    public async Task ConfirmRedemptionAsync_Should_UseCanonicalRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (route, request, _) =>
            {
                route.Should().Be(ApiRoutes.Loyalty.ConfirmRedemption);
                request.Should().BeOfType<ConfirmRedemptionRequest>();

                return Task.FromResult<object?>(Result<ConfirmRedemptionResponse>.Ok(new ConfirmRedemptionResponse
                {
                    Success = true,
                    ErrorMessage = null,
                    ErrorCode = null,
                    NewBalance = 15
                }));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.ConfirmRedemptionAsync("token", TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.PointsBalance.Should().Be(15);
    }

    [Fact]
    public async Task ConfirmRedemptionAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (_, _, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.ConfirmRedemptionAsync("token", TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("confirming redemption"));
    }

    [Fact]
    public async Task ProcessScanSessionForBusinessAsync_Should_Fail_WhenTokenIsEmpty()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.ProcessScanSessionForBusinessAsync(" ", TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("QR token is required.");
    }

    [Fact]
    public async Task ProcessScanSessionForBusinessAsync_Should_UseCanonicalRouteAndMapActions()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (route, _, _) =>
            {
                route.Should().Be("api/v1/business/loyalty/scan/process");
                return Task.FromResult<object?>(Result<ProcessScanSessionForBusinessResponse>.Ok(new ProcessScanSessionForBusinessResponse
                {
                    Mode = LoyaltyScanMode.Accrual,
                    BusinessId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    BusinessLocationId = null,
                    CustomerDisplayName = "Max",
                    AllowedActions = LoyaltyScanAllowedActions.CanConfirmAccrual,
                    SelectedRewards = Array.Empty<LoyaltyRewardSummary>()
                }));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);

        var result = await service.ProcessScanSessionForBusinessAsync("token_123", TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CanConfirmAccrual.Should().BeTrue();
        result.Value.CanConfirmRedemption.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessScanSessionForBusinessAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (_, _, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.ProcessScanSessionForBusinessAsync("token_123", TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("processing scan"));
    }

    [Fact]
    public async Task TrackPromotionInteractionAsync_Should_Fail_WhenRequestIsNull()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.TrackPromotionInteractionAsync(null!, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Request body is required.");
    }

    [Fact]
    public async Task TrackPromotionInteractionAsync_Should_Fail_WhenBusinessIdMissing()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.TrackPromotionInteractionAsync(
            new TrackPromotionInteractionRequest
            {
                BusinessId = Guid.Empty,
                Title = "Welcome"
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessId is required.");
    }

    [Fact]
    public async Task TrackPromotionInteractionAsync_Should_Fail_WhenTitleMissing()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.TrackPromotionInteractionAsync(
            new TrackPromotionInteractionRequest
            {
                BusinessId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                Title = " "
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Title is required.");
    }

    [Fact]
    public async Task TrackPromotionInteractionAsync_Should_UseCanonicalRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnPostNoContentAsync = (route, _, _) =>
            {
                route.Should().Be("api/v1/member/loyalty/my/promotions/track");
                return Task.FromResult<object?>(Result.Ok());
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.TrackPromotionInteractionAsync(
            new TrackPromotionInteractionRequest
            {
                BusinessId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                BusinessName = "Demo Coffee",
                Title = "Welcome",
                CtaKind = "detail"
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task TrackPromotionInteractionAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPostNoContentAsync = (_, _, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.TrackPromotionInteractionAsync(
            new TrackPromotionInteractionRequest
            {
                BusinessId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                Title = "Welcome"
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("tracking promotion interaction"));
    }

    [Fact]
    public async Task GetMyAccountsAsync_Should_UseCanonicalRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (route, _) =>
            {
                route.Should().Be(ApiRoutes.Loyalty.GetMyAccounts);
                return Task.FromResult<object?>(Result<IReadOnlyList<LoyaltyAccountSummary>>.Ok(new List<LoyaltyAccountSummary>
                {
                    new()
                    {
                        BusinessId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                        BusinessName = "Demo Coffee"
                    }
                }));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetMyAccountsAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMyAccountsAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetMyAccountsAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving accounts"));
    }

    [Fact]
    public async Task GetMyOverviewAsync_Should_Fail_WhenApiReturnsFailure()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) => Task.FromResult<object?>(Result<MyLoyaltyOverviewResponse>.Fail("bad request"))
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetMyOverviewAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("bad request");
    }

    [Fact]
    public async Task GetMyOverviewAsync_Should_UseCanonicalRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (route, _) =>
            {
                route.Should().Be(ApiRoutes.Loyalty.GetMyOverview);
                return Task.FromResult<object?>(Result<MyLoyaltyOverviewResponse>.Ok(new MyLoyaltyOverviewResponse()));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetMyOverviewAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMyOverviewAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetMyOverviewAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving loyalty overview"));
    }

    [Fact]
    public async Task GetMyHistoryAsync_Should_Fail_WhenBusinessIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.GetMyHistoryAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid business identifier.");
    }

    [Fact]
    public async Task GetMyHistoryAsync_Should_UseCanonicalRoute()
    {
        var businessId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (route, _) =>
            {
                route.Should().Be($"api/v1/member/loyalty/my/history/{businessId:D}");
                return Task.FromResult<object?>(Result<IReadOnlyList<PointsTransaction>>.Ok(Array.Empty<PointsTransaction>()));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetMyHistoryAsync(businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyHistoryAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetMyHistoryAsync(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving history"));
    }

    [Fact]
    public async Task GetBusinessDashboardAsync_Should_UseCanonicalRoute()
    {
        var businessId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (route, _) =>
            {
                route.Should().Be($"api/v1/member/loyalty/business/{businessId:D}/dashboard");
                return Task.FromResult<object?>(Result<MyLoyaltyBusinessDashboard>.Ok(new MyLoyaltyBusinessDashboard()));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetBusinessDashboardAsync(businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBusinessDashboardAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetBusinessDashboardAsync(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving loyalty dashboard"));
    }

    [Fact]
    public async Task GetMyLoyaltyTimelinePageAsync_Should_Fail_WhenRequestIsNull()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.GetMyLoyaltyTimelinePageAsync(null!, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Request body is required.");
    }

    [Fact]
    public async Task GetMyLoyaltyTimelinePageAsync_Should_UseCanonicalRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (route, request, _) =>
            {
                route.Should().Be(ApiRoutes.Loyalty.GetMyTimeline);
                request.Should().BeOfType<GetMyLoyaltyTimelinePageRequest>();
                return Task.FromResult<object?>(Result<GetMyLoyaltyTimelinePageResponse>.Ok(new GetMyLoyaltyTimelinePageResponse()));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetMyLoyaltyTimelinePageAsync(new GetMyLoyaltyTimelinePageRequest(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMyLoyaltyTimelinePageAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (_, _, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetMyLoyaltyTimelinePageAsync(new GetMyLoyaltyTimelinePageRequest(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving timeline"));
    }

    [Fact]
    public async Task GetNextRewardAsync_Should_Fail_WhenBusinessIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.GetNextRewardAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid business identifier.");
    }

    [Fact]
    public async Task GetNextRewardAsync_Should_ReturnNull_WhenNoContent()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) =>
                Task.FromResult<object?>(Result<LoyaltyRewardSummary>.Fail(ApiClient.NoContentResultMessage))
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetNextRewardAsync(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetNextRewardAsync_Should_MapError_WhenRequestFails()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) => throw new TimeoutException("network down")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetNextRewardAsync(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving next reward"));
    }

    [Fact]
    public async Task GetMyPromotionsAsync_Should_Fail_WhenRequestIsNull()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.GetMyPromotionsAsync(null!, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Request body is required.");
    }

    [Fact]
    public async Task GetMyPromotionsAsync_Should_UseCanonicalRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (route, request, _) =>
            {
                route.Should().Be(ApiRoutes.Loyalty.GetMyPromotions);
                request.Should().BeOfType<MyPromotionsRequest>();
                return Task.FromResult<object?>(Result<MyPromotionsResponse>.Ok(new MyPromotionsResponse()));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetMyPromotionsAsync(new MyPromotionsRequest(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMyPromotionsAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (_, _, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetMyPromotionsAsync(new MyPromotionsRequest(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving promotions"));
    }

    [Fact]
    public async Task GetBusinessRewardConfigurationAsync_Should_UseCanonicalRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (route, _) =>
            {
                route.Should().Be(ApiRoutes.Loyalty.GetBusinessRewardConfiguration);
                return Task.FromResult<object?>(Result<BusinessRewardConfigurationResponse>.Ok(new BusinessRewardConfigurationResponse()));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetBusinessRewardConfigurationAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBusinessRewardConfigurationAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetBusinessRewardConfigurationAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving business reward configuration"));
    }

    [Fact]
    public async Task CreateBusinessRewardTierAsync_Should_UseCanonicalRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (route, request, _) =>
            {
                route.Should().Be(ApiRoutes.Loyalty.CreateBusinessRewardTier);
                request.Should().BeOfType<CreateBusinessRewardTierRequest>();
                return Task.FromResult<object?>(Result<BusinessRewardTierMutationResponse>.Ok(new BusinessRewardTierMutationResponse()));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.CreateBusinessRewardTierAsync(new CreateBusinessRewardTierRequest(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateBusinessRewardTierAsync_Should_Fail_WhenRequestIsNull()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.CreateBusinessRewardTierAsync(null!, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Request body is required.");
    }

    [Fact]
    public async Task CreateBusinessRewardTierAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (_, _, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.CreateBusinessRewardTierAsync(new CreateBusinessRewardTierRequest(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("creating reward tier"));
    }

    [Fact]
    public async Task UpdateBusinessRewardTierAsync_Should_UseCanonicalRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnPutResultAsync = (route, request, _) =>
            {
                route.Should().Be(ApiRoutes.Loyalty.UpdateBusinessRewardTier);
                request.Should().BeOfType<UpdateBusinessRewardTierRequest>();
                return Task.FromResult<object?>(Result<BusinessRewardTierMutationResponse>.Ok(new BusinessRewardTierMutationResponse()));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.UpdateBusinessRewardTierAsync(new UpdateBusinessRewardTierRequest(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateBusinessRewardTierAsync_Should_Fail_WhenRequestIsNull()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.UpdateBusinessRewardTierAsync(null!, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Request body is required.");
    }

    [Fact]
    public async Task UpdateBusinessRewardTierAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPutResultAsync = (_, _, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.UpdateBusinessRewardTierAsync(new UpdateBusinessRewardTierRequest(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("updating reward tier"));
    }

    [Fact]
    public async Task DeleteBusinessRewardTierAsync_Should_UseCanonicalRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (route, request, _) =>
            {
                route.Should().Be(ApiRoutes.Loyalty.DeleteBusinessRewardTier);
                request.Should().BeOfType<DeleteBusinessRewardTierRequest>();
                return Task.FromResult<object?>(Result<BusinessRewardTierMutationResponse>.Ok(new BusinessRewardTierMutationResponse()));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.DeleteBusinessRewardTierAsync(new DeleteBusinessRewardTierRequest(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteBusinessRewardTierAsync_Should_Fail_WhenRequestIsNull()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.DeleteBusinessRewardTierAsync(null!, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Request body is required.");
    }

    [Fact]
    public async Task DeleteBusinessRewardTierAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (_, _, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.DeleteBusinessRewardTierAsync(new DeleteBusinessRewardTierRequest(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("deleting reward tier"));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(15, 200)]
    public async Task GetBusinessCampaignsAsync_Should_NormalizePagination(int page, int pageSize)
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (route, _) =>
            {
                var normalizedPage = Math.Max(page, 1);
                var normalizedPageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
                route.Should().Be($"api/v1/business/loyalty/campaigns?page={normalizedPage}&pageSize={normalizedPageSize}");
                return Task.FromResult<object?>(Result<GetBusinessCampaignsResponse>.Ok(new GetBusinessCampaignsResponse()));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetBusinessCampaignsAsync(page, pageSize, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task CreateBusinessCampaignAsync_Should_UseCanonicalRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (route, request, _) =>
            {
                route.Should().Be(ApiRoutes.Loyalty.GetBusinessCampaigns);
                request.Should().BeOfType<CreateBusinessCampaignRequest>();
                return Task.FromResult<object?>(Result<BusinessCampaignMutationResponse>.Ok(new BusinessCampaignMutationResponse()));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.CreateBusinessCampaignAsync(new CreateBusinessCampaignRequest(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateBusinessCampaignAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (_, _, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.CreateBusinessCampaignAsync(new CreateBusinessCampaignRequest(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("creating business campaign"));
    }

    [Fact]
    public async Task UpdateBusinessCampaignAsync_Should_Fail_WhenCampaignIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.UpdateBusinessCampaignAsync(
            new UpdateBusinessCampaignRequest { Id = Guid.Empty },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Campaign Id is required.");
    }

    [Fact]
    public async Task UpdateBusinessCampaignAsync_Should_UseCanonicalRoute()
    {
        var campaignId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var apiClient = new FakeApiClient
        {
            OnPutNoContentAsync = (route, request, _) =>
            {
                route.Should().Be(ApiRoutes.Loyalty.UpdateBusinessCampaign(campaignId));
                request.Should().BeOfType<UpdateBusinessCampaignRequest>();
                return Task.FromResult<object?>(Result.Ok());
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.UpdateBusinessCampaignAsync(
            new UpdateBusinessCampaignRequest
            {
                Id = campaignId,
                Name = "Summer Sale"
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBusinessCampaignAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPutNoContentAsync = (_, _, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.UpdateBusinessCampaignAsync(
            new UpdateBusinessCampaignRequest { Id = Guid.Parse("11111111-2222-3333-4444-555555555555") },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("updating business campaign"));
    }

    [Fact]
    public async Task SetBusinessCampaignActivationAsync_Should_Fail_WhenCampaignIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.SetBusinessCampaignActivationAsync(
            new SetCampaignActivationRequest { Id = Guid.Empty },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Campaign Id is required.");
    }

    [Fact]
    public async Task SetBusinessCampaignActivationAsync_Should_UseCanonicalRoute()
    {
        var campaignId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var apiClient = new FakeApiClient
        {
            OnPostNoContentAsync = (route, request, _) =>
            {
                route.Should().Be(ApiRoutes.Loyalty.SetBusinessCampaignActivation(campaignId));
                request.Should().BeOfType<SetCampaignActivationRequest>();
                return Task.FromResult<object?>(Result.Ok());
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.SetBusinessCampaignActivationAsync(
            new SetCampaignActivationRequest { Id = campaignId },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task SetBusinessCampaignActivationAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPostNoContentAsync = (_, _, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.SetBusinessCampaignActivationAsync(
            new SetCampaignActivationRequest { Id = Guid.Parse("11111111-2222-3333-4444-555555555555") },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("setting campaign activation"));
    }

    [Fact]
    public async Task GetCurrentBusinessSubscriptionStatusAsync_Should_UseCanonicalRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) =>
                Task.FromResult<object?>(Result<BusinessSubscriptionStatusResponse>.Ok(new BusinessSubscriptionStatusResponse
                {
                    HasSubscription = true,
                    SubscriptionId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    Status = "Active",
                    PlanName = "Startup",
                    Provider = "Stripe",
                    CancelAtPeriodEnd = false
                }))
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetCurrentBusinessSubscriptionStatusAsync(TestContext.Current.CancellationToken);

        apiClient.Route.Should().Be(ApiRoutes.Billing.GetCurrentBusinessSubscription);
        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.HasSubscription.Should().BeTrue();
        result.Value.PlanName.Should().Be("Startup");
    }

    [Fact]
    public async Task GetCurrentBusinessSubscriptionStatusAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetCurrentBusinessSubscriptionStatusAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("loading subscription status"));
    }

    [Fact]
    public async Task SetCancelAtPeriodEndAsync_Should_Fail_WhenRequestIsNull()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.SetCancelAtPeriodEndAsync(null!, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Request body is required.");
    }

    [Fact]
    public async Task SetCancelAtPeriodEndAsync_Should_Fail_WhenSubscriptionIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.SetCancelAtPeriodEndAsync(
            new SetCancelAtPeriodEndRequest
            {
                SubscriptionId = Guid.Empty,
                CancelAtPeriodEnd = true,
                RowVersion = Array.Empty<byte>()
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Subscription Id is required.");
    }

    [Fact]
    public async Task SetCancelAtPeriodEndAsync_Should_UseCanonicalRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (route, request, _) =>
            {
                route.Should().Be(ApiRoutes.Billing.SetCancelAtPeriodEnd);
                request.Should().BeOfType<SetCancelAtPeriodEndRequest>();
                return Task.FromResult<object?>(Result<SetCancelAtPeriodEndResponse>.Ok(new SetCancelAtPeriodEndResponse
                {
                    SubscriptionId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    CancelAtPeriodEnd = true,
                    RowVersion = new byte[] { 1, 2, 3 }
                }));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.SetCancelAtPeriodEndAsync(
            new SetCancelAtPeriodEndRequest
            {
                SubscriptionId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                CancelAtPeriodEnd = true,
                RowVersion = new byte[] { 9 }
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CancelAtPeriodEnd.Should().BeTrue();
        result.Value!.SubscriptionId.Should().Be(Guid.Parse("11111111-2222-3333-4444-555555555555"));
    }

    [Fact]
    public async Task SetCancelAtPeriodEndAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (_, _, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.SetCancelAtPeriodEndAsync(
            new SetCancelAtPeriodEndRequest
            {
                SubscriptionId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                CancelAtPeriodEnd = true,
                RowVersion = new byte[] { 1, 2 }
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("updating cancellation preference"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetBillingPlansAsync_Should_UseCanonicalRoute(bool activeOnly)
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (route, _) =>
            {
                route.Should().Be($"api/v1/business/billing/plans?activeOnly={activeOnly.ToString().ToLowerInvariant()}");
                return Task.FromResult<object?>(Result<GetBillingPlansResponse>.Ok(new GetBillingPlansResponse
                {
                    Items = new List<BillingPlanSummary>
                    {
                        new()
                        {
                            Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                            Name = "Starter",
                            Currency = "EUR",
                            PriceMinor = 990,
                            IsActive = true
                        }
                    }
                }));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetBillingPlansAsync(activeOnly, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetBillingPlansAsync_Should_ReturnNetworkFailure_OnException(bool activeOnly)
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetBillingPlansAsync(activeOnly, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving billing plans"));
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutIntentAsync_Should_Fail_WhenRequestIsNull()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.CreateSubscriptionCheckoutIntentAsync(null!, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Request body is required.");
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutIntentAsync_Should_Fail_WhenPlanIdIsEmpty()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.CreateSubscriptionCheckoutIntentAsync(
            new CreateSubscriptionCheckoutIntentRequest { PlanId = Guid.Empty },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Plan Id is required.");
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutIntentAsync_Should_UseCanonicalRouteAndMapResponse()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (route, request, _) =>
            {
                route.Should().Be(ApiRoutes.Billing.CreateCheckoutIntent);
                request.Should().BeOfType<CreateSubscriptionCheckoutIntentRequest>();

                var payload = (CreateSubscriptionCheckoutIntentRequest)request;
                payload.PlanId.Should().Be(Guid.Parse("11111111-2222-3333-4444-555555555555"));

                return Task.FromResult<object?>(Result<CreateSubscriptionCheckoutIntentResponse>.Ok(new CreateSubscriptionCheckoutIntentResponse
                {
                    CheckoutUrl = "https://checkout.example/initiate",
                    ExpiresAtUtc = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                    Provider = "Stripe",
                    ProviderCheckoutSessionReference = "pi-test",
                    ProviderSubscriptionReference = null
                }));
            }
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.CreateSubscriptionCheckoutIntentAsync(
            new CreateSubscriptionCheckoutIntentRequest
            {
                PlanId = Guid.Parse("11111111-2222-3333-4444-555555555555")
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CheckoutUrl.Should().Be("https://checkout.example/initiate");
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutIntentAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnPostResultAsync = (_, _, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.CreateSubscriptionCheckoutIntentAsync(
            new CreateSubscriptionCheckoutIntentRequest
            {
                PlanId = Guid.Parse("11111111-2222-3333-4444-555555555555")
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("creating checkout intent"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task GetMyBusinessesAsync_Should_Fail_WhenPageSizeIsInvalid(int pageSize)
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.GetMyBusinessesAsync(1, pageSize, false, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("PageSize must be between 1 and 200.");
    }

    [Fact]
    public async Task GetMyBusinessesAsync_Should_Fail_WhenPageIsInvalid()
    {
        var service = CreateService(new FakeApiClient(), TimeProvider.System);

        var result = await service.GetMyBusinessesAsync(0, 20, false, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Page must be a positive integer.");
    }

    [Fact]
    public async Task GetMyBusinessesAsync_Should_UseCanonicalRoute()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (route, _) =>
                Task.FromResult<object?>(Result<MyLoyaltyBusinessesResponse>.Ok(new MyLoyaltyBusinessesResponse
                {
                    Items = Array.Empty<MyLoyaltyBusinessSummary>(),
                    Total = 0,
                    Request = new PagedRequest { Page = 1, PageSize = 20 }
                }))
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetMyBusinessesAsync(1, 20, true, TestContext.Current.CancellationToken);

        apiClient.Route.Should().Be("api/v1/member/loyalty/my/businesses?page=1&pageSize=20&includeInactiveBusinesses=true");
        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetMyBusinessesAsync_Should_ReturnNetworkFailure_OnException()
    {
        var apiClient = new FakeApiClient
        {
            OnGetResultAsync = (_, _) => throw new TimeoutException("network failure")
        };

        var service = CreateService(apiClient, TimeProvider.System);
        var result = await service.GetMyBusinessesAsync(1, 20, false, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(MobileErrorMessages.NetworkFailure("retrieving my businesses"));
    }

    private static LoyaltyService CreateService(FakeApiClient apiClient, TimeProvider timeProvider)
    {
        return new LoyaltyService(apiClient, timeProvider);
    }

    private sealed class FakeApiClient : IApiClient
    {
        public string? Route { get; private set; }

        public Func<string, object, CancellationToken, Task<object?>>? OnPostResultAsync { get; init; }

        public Func<string, CancellationToken, Task<object?>>? OnGetResultAsync { get; init; }

        public Func<string, object, CancellationToken, Task<object?>>? OnPostNoContentAsync { get; init; }

        public Func<string, object, CancellationToken, Task<object?>>? OnPutNoContentAsync { get; init; }

        public Func<string, object, CancellationToken, Task<object?>>? OnPutResultAsync { get; init; }

        public void SetBearerToken(string? accessToken)
        {
        }

        public Task<Result<TResponse>> GetResultAsync<TResponse>(string route, CancellationToken ct)
        {
            Route = route;
            if (OnGetResultAsync is null)
            {
                return Task.FromResult(Result<TResponse>.Fail("No GET handler configured."));
            }

            return OnGetResultAsync(route, ct).ContinueWith(
                t => t.Result as Result<TResponse> ?? Result<TResponse>.Fail("Unexpected GET result type."));
        }

        public Task<Result<string>> GetStringResultAsync(string route, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PostResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
        {
            Route = route;
            if (OnPostResultAsync is null)
            {
                return Task.FromResult(Result<TResponse>.Fail("No POST handler configured."));
            }

            return OnPostResultAsync(route, request!, ct).ContinueWith(
                t => t.Result as Result<TResponse> ?? Result<TResponse>.Fail("Unexpected POST result type."));
        }

        public Task<Result<TResponse>> GetEnvelopeResultAsync<TResponse>(string route, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PostFileResultAsync<TResponse>(
            string route,
            Stream fileStream,
            string formFieldName,
            string fileName,
            string contentType,
            CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PostEnvelopeResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<TResponse?> GetAsync<TResponse>(string route, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<TResponse?> PostAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PutResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
        {
            Route = route;
            if (OnPutResultAsync is null)
            {
                return Task.FromResult(Result<TResponse>.Fail("No PUT handler configured."));
            }

            return OnPutResultAsync(route, request!, ct).ContinueWith(
                t => t.Result as Result<TResponse> ?? Result<TResponse>.Fail("Unexpected PUT result type."));
        }

        public Task<TResponse?> PutAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result> PutNoContentAsync<TRequest>(string route, TRequest request, CancellationToken ct)
        {
            Route = route;
            if (OnPutNoContentAsync is null)
            {
                return Task.FromResult(Result.Fail("No PUT handler configured."));
            }

            return OnPutNoContentAsync(route, request!, ct).ContinueWith(
                t => t.Result as Result ?? Result.Fail("Unexpected PUT result type."));
        }

        public Task<Result> PostNoContentAsync<TRequest>(string route, TRequest request, CancellationToken ct)
        {
            Route = route;
            if (OnPostNoContentAsync is null)
            {
                return Task.FromResult(Result.Fail("No POST handler configured."));
            }

            return OnPostNoContentAsync(route, request!, ct).ContinueWith(
                t => t.Result as Result ?? Result.Fail("Unexpected POST result type."));
        }
    }
}
