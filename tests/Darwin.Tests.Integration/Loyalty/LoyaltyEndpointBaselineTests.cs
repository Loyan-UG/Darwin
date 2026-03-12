using Darwin.Contracts.Loyalty;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

using Darwin.Tests.Common.TestInfrastructure;
using Darwin.Tests.Integration.TestInfrastructure;

namespace Darwin.Tests.Integration.Loyalty;

/// <summary>
///     Provides baseline authorization integration coverage for loyalty endpoints.
///     These tests ensure anonymous callers are blocked before any business logic
///     handlers are executed.
/// </summary>
public sealed class LoyaltyEndpointBaselineTests : DeterministicIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    /// <summary>
    ///     Initializes the test suite with a host configured for Testing environment.
    /// </summary>
    /// <param name="factory">Shared host factory used to create isolated clients.</param>
    public LoyaltyEndpointBaselineTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    /// <summary>
    ///     Recreates and seeds the test database before each test class to guarantee
    ///     deterministic state regardless of execution order across integration suites.
    /// </summary>
    public Task InitializeAsync() => IntegrationTestDatabaseReset.ResetAndSeedAsync(_factory);

    /// <summary>
    ///     No asynchronous class-level cleanup is required because each test class
    ///     uses isolated clients and reset logic runs during initialization.
    /// </summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    ///     Verifies that listing the current user's loyalty businesses requires an
    ///     authenticated caller and rejects anonymous requests.
    /// </summary>
    [Fact]
    public async Task GetMyBusinesses_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();

        // Act
        using var response = await client.GetAsync("/api/v1/loyalty/my/businesses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Verifies that listing loyalty accounts for the current member is protected
    ///     by authentication and rejects anonymous requests.
    /// </summary>
    [Fact]
    public async Task GetMyAccounts_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();

        // Act
        using var response = await client.GetAsync("/api/v1/loyalty/my/accounts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Verifies that preparing a scan session requires authentication and does
    ///     not allow anonymous requests to enter loyalty session flow.
    /// </summary>
    [Fact]
    public async Task PrepareScanSession_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var request = new PrepareScanSessionRequest
        {
            BusinessId = Guid.NewGuid(),
            Mode = LoyaltyScanMode.Accrual,
            SelectedRewardTierIds = []
        };

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/loyalty/scan/prepare", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Verifies that processing scan sessions in business flow is protected and
    ///     cannot be accessed anonymously.
    /// </summary>
    [Fact]
    public async Task ProcessScanSession_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var request = new ProcessScanSessionForBusinessRequest
        {
            ScanSessionToken = $"anonymous-test-token-{Guid.NewGuid():N}"
        };

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/loyalty/scan/process", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Verifies that accrual confirmation endpoint is also guarded by authentication
    ///     and rejects anonymous requests before handler invocation.
    /// </summary>
    [Fact]
    public async Task ConfirmAccrual_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var request = new ConfirmAccrualRequest
        {
            ScanSessionToken = $"anonymous-test-token-{Guid.NewGuid():N}",
            Points = 1,
            Note = "anonymous-call-should-fail"
        };

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/loyalty/scan/confirm-accrual", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }



    /// <summary>
    ///     Verifies that redemption confirmation endpoint is protected by authentication
    ///     and rejects anonymous requests before business rules are evaluated.
    /// </summary>
    [Fact]
    public async Task ConfirmRedemption_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var request = new ConfirmRedemptionRequest
        {
            ScanSessionToken = $"anonymous-test-token-{Guid.NewGuid():N}"
        };

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/loyalty/scan/confirm-redemption", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Verifies that timeline endpoint is protected and rejects anonymous requests,
    ///     preventing unauthenticated access to loyalty activity history.
    /// </summary>
    [Fact]
    public async Task GetMyTimeline_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var request = new GetMyLoyaltyTimelinePageRequest
        {
            BusinessId = null,
            PageSize = 20,
            BeforeAtUtc = null,
            BeforeId = null
        };

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/loyalty/my/timeline", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }



    /// <summary>
    ///     Verifies that promotions feed endpoint is protected and cannot be
    ///     queried by anonymous callers.
    /// </summary>
    [Fact]
    public async Task GetMyPromotions_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var request = new MyPromotionsRequest
        {
            BusinessId = null,
            MaxItems = 20
        };

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/loyalty/my/promotions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }





    /// <summary>
    ///     Verifies that business-specific loyalty account snapshot endpoint is protected
    ///     and rejects anonymous requests.
    /// </summary>
    [Fact]
    public async Task GetAccountForBusiness_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var businessId = Guid.NewGuid();

        // Act
        using var response = await client.GetAsync($"/api/v1/loyalty/account/{businessId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Verifies that join-loyalty endpoint requires authentication and rejects
    ///     anonymous requests attempting to create a loyalty account.
    /// </summary>
    [Fact]
    public async Task JoinLoyaltyAccount_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var businessId = Guid.NewGuid();
        var request = new JoinLoyaltyRequest
        {
            BusinessLocationId = null
        };

        // Act
        using var response = await client.PostAsJsonAsync($"/api/v1/loyalty/account/{businessId}/join", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }



    /// <summary>
    ///     Verifies that next-reward lookup endpoint is protected and rejects
    ///     anonymous requests to prevent unauthorized account insight exposure.
    /// </summary>
    [Fact]
    public async Task GetNextReward_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var businessId = Guid.NewGuid();

        // Act
        using var response = await client.GetAsync($"/api/v1/loyalty/account/{businessId}/next-reward");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }



    /// <summary>
    ///     Verifies that per-business loyalty history endpoint is protected and
    ///     rejects anonymous requests.
    /// </summary>
    [Fact]
    public async Task GetMyHistory_Should_ReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var businessId = Guid.NewGuid();

        // Act
        using var response = await client.GetAsync($"/api/v1/loyalty/my/history/{businessId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Creates an HTTPS client so status assertions are not affected by HTTP->HTTPS redirect behavior.
    /// </summary>
    /// <returns>Configured HttpClient instance.</returns>
    private HttpClient CreateHttpsClient()
    {
        return CreateHttpsClient();
    }
}
