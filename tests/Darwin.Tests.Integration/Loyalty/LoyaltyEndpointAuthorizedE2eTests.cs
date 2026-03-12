using Darwin.Contracts.Common;
using Darwin.Contracts.Loyalty;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Darwin.Tests.Common.TestInfrastructure;

namespace Darwin.Tests.Integration.Loyalty;

/// <summary>
///     Provides authorized end-to-end integration coverage for the loyalty scan-session flow.
///     These tests exercise real multi-step journeys across consumer and business roles:
///     prepare, process, and confirm for both accrual and redemption modes.
/// </summary>
public sealed class LoyaltyEndpointAuthorizedE2eTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    ///     Initializes the test fixture with a host configured for Testing environment.
    /// </summary>
    /// <param name="factory">Shared WebApplicationFactory instance.</param>
    public LoyaltyEndpointAuthorizedE2eTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    /// <summary>
    ///     Verifies the full authorized accrual journey:
    ///     1) consumer prepares a scan session,
    ///     2) business processes the scanned token,
    ///     3) business confirms accrual,
    ///     4) consumer account balance is updated.
    /// </summary>
    [Fact]
    public async Task PrepareProcessConfirmAccrual_Should_Succeed_EndToEnd_WhenAuthorized()
    {
        // Arrange
        using var consumerClient = IntegrationTestClientFactory.CreateHttpsClient(_factory);
        using var businessClient = IntegrationTestClientFactory.CreateHttpsClient(_factory);

        var consumerToken = await IdentityFlowTestHelper.LoginExpectSuccessAsync(consumerClient, SeedConsumerEmail, SeedConsumerPassword, SeedConsumerDeviceId);
        var businessToken = await IdentityFlowTestHelper.LoginExpectSuccessAsync(businessClient, SeedBusinessEmail, SeedBusinessPassword, SeedBusinessDeviceId);

        var businessId = ExtractBusinessIdFromAccessToken(businessToken.AccessToken);
        businessId.Should().NotBe(Guid.Empty);

        consumerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", consumerToken.AccessToken);
        businessClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", businessToken.AccessToken);

        // Ensure consumer has an account for the same business used by the business token.
        await JoinBusinessIfNeededAsync(consumerClient, businessId);
        var accountBefore = await GetAccountForBusinessAsync(consumerClient, businessId);

        // Step 1: Prepare scan session in accrual mode as consumer.
        using var prepareResponse = await consumerClient.PostAsJsonAsync("/api/v1/loyalty/scan/prepare", new PrepareScanSessionRequest
        {
            BusinessId = businessId,
            Mode = LoyaltyScanMode.Accrual,
            SelectedRewardTierIds = []
        });

        prepareResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var prepareBody = await prepareResponse.Content.ReadFromJsonAsync<PrepareScanSessionResponse>();
        prepareBody.Should().NotBeNull();
        prepareBody!.ScanSessionToken.Should().NotBeNullOrWhiteSpace();
        prepareBody.Mode.Should().Be(LoyaltyScanMode.Accrual);

        // Step 2: Process the same token as business app.
        using var processResponse = await businessClient.PostAsJsonAsync("/api/v1/loyalty/scan/process", new ProcessScanSessionForBusinessRequest
        {
            ScanSessionToken = prepareBody.ScanSessionToken
        });

        processResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var processBody = await processResponse.Content.ReadFromJsonAsync<ProcessScanSessionForBusinessResponse>();
        processBody.Should().NotBeNull();
        processBody!.Mode.Should().Be(LoyaltyScanMode.Accrual);
        processBody.BusinessId.Should().Be(businessId);

        // Step 3: Confirm accrual as business app.
        const int awardedPoints = 7;
        using var confirmResponse = await businessClient.PostAsJsonAsync("/api/v1/loyalty/scan/confirm-accrual", new ConfirmAccrualRequest
        {
            ScanSessionToken = prepareBody.ScanSessionToken,
            Points = awardedPoints,
            Note = "Integration accrual confirmation."
        });

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmBody = await confirmResponse.Content.ReadFromJsonAsync<ConfirmAccrualResponse>();
        confirmBody.Should().NotBeNull();
        confirmBody!.Success.Should().BeTrue();

        // Step 4: Verify updated balance from consumer perspective.
        var accountAfter = await GetAccountForBusinessAsync(consumerClient, businessId);
        accountAfter.PointsBalance.Should().BeGreaterThanOrEqualTo(accountBefore.PointsBalance + awardedPoints);
    }

    /// <summary>
    ///     Verifies the full authorized redemption journey:
    ///     1) consumer ensures sufficient points,
    ///     2) consumer prepares redemption session with selected reward tier,
    ///     3) business processes the token,
    ///     4) business confirms redemption.
    /// </summary>
    [Fact]
    public async Task PrepareProcessConfirmRedemption_Should_Succeed_EndToEnd_WhenAuthorized()
    {
        // Arrange
        using var consumerClient = IntegrationTestClientFactory.CreateHttpsClient(_factory);
        using var businessClient = IntegrationTestClientFactory.CreateHttpsClient(_factory);

        var consumerToken = await IdentityFlowTestHelper.LoginExpectSuccessAsync(consumerClient, SeedConsumerEmail, SeedConsumerPassword, SeedConsumerDeviceId);
        var businessToken = await IdentityFlowTestHelper.LoginExpectSuccessAsync(businessClient, SeedBusinessEmail, SeedBusinessPassword, SeedBusinessDeviceId);

        var businessId = ExtractBusinessIdFromAccessToken(businessToken.AccessToken);
        businessId.Should().NotBe(Guid.Empty);

        consumerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", consumerToken.AccessToken);
        businessClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", businessToken.AccessToken);

        // Ensure consumer account exists for target business.
        await JoinBusinessIfNeededAsync(consumerClient, businessId);

        // Ensure consumer has enough points to make at least one reward selectable.
        await EnsureSufficientPointsForRedemptionAsync(consumerClient, businessClient, businessId, minimumExtraPoints: 300);

        var rewards = await GetRewardsForBusinessAsync(consumerClient, businessId);
        var selectedReward = rewards.FirstOrDefault(r => r.IsSelectable);
        selectedReward.Should().NotBeNull("consumer must have at least one selectable reward after preparatory accrual");

        // Step 1: Prepare redemption scan session as consumer.
        using var prepareResponse = await consumerClient.PostAsJsonAsync("/api/v1/loyalty/scan/prepare", new PrepareScanSessionRequest
        {
            BusinessId = businessId,
            Mode = LoyaltyScanMode.Redemption,
            SelectedRewardTierIds = [selectedReward!.LoyaltyRewardTierId]
        });

        prepareResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var prepareBody = await prepareResponse.Content.ReadFromJsonAsync<PrepareScanSessionResponse>();
        prepareBody.Should().NotBeNull();
        prepareBody!.ScanSessionToken.Should().NotBeNullOrWhiteSpace();
        prepareBody.Mode.Should().Be(LoyaltyScanMode.Redemption);

        // Step 2: Process the redemption token as business app.
        using var processResponse = await businessClient.PostAsJsonAsync("/api/v1/loyalty/scan/process", new ProcessScanSessionForBusinessRequest
        {
            ScanSessionToken = prepareBody.ScanSessionToken
        });

        processResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var processBody = await processResponse.Content.ReadFromJsonAsync<ProcessScanSessionForBusinessResponse>();
        processBody.Should().NotBeNull();
        processBody!.Mode.Should().Be(LoyaltyScanMode.Redemption);
        processBody.BusinessId.Should().Be(businessId);
        processBody.SelectedRewards.Should().NotBeNull();
        processBody.SelectedRewards.Should().NotBeEmpty();

        // Step 3: Confirm redemption as business app.
        using var confirmResponse = await businessClient.PostAsJsonAsync("/api/v1/loyalty/scan/confirm-redemption", new ConfirmRedemptionRequest
        {
            ScanSessionToken = prepareBody.ScanSessionToken
        });

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmBody = await confirmResponse.Content.ReadFromJsonAsync<ConfirmRedemptionResponse>();
        confirmBody.Should().NotBeNull();
        confirmBody!.Success.Should().BeTrue();
    }

    /// <summary>
    ///     Ensures a consumer loyalty account exists for the target business.
    ///     The endpoint is idempotent and returns existing account when already joined.
    /// </summary>
    /// <param name="consumerClient">Authorized consumer HTTP client.</param>
    /// <param name="businessId">Target business identifier.</param>
    private static async Task JoinBusinessIfNeededAsync(HttpClient consumerClient, Guid businessId)
    {
        using var joinResponse = await consumerClient.PostAsJsonAsync($"/api/v1/loyalty/account/{businessId}/join", new JoinLoyaltyRequest
        {
            BusinessLocationId = null
        });

        joinResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var joinBody = await joinResponse.Content.ReadFromJsonAsync<LoyaltyAccountSummary>();
        joinBody.Should().NotBeNull();
        joinBody!.BusinessId.Should().Be(businessId);
    }

    /// <summary>
    ///     Reads the current consumer loyalty account for a business.
    /// </summary>
    /// <param name="consumerClient">Authorized consumer HTTP client.</param>
    /// <param name="businessId">Target business identifier.</param>
    /// <returns>Current account summary for the specified business.</returns>
    private static async Task<LoyaltyAccountSummary> GetAccountForBusinessAsync(HttpClient consumerClient, Guid businessId)
    {
        using var accountResponse = await consumerClient.GetAsync($"/api/v1/loyalty/account/{businessId}");
        accountResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var account = await accountResponse.Content.ReadFromJsonAsync<LoyaltyAccountSummary>();
        account.Should().NotBeNull();
        account!.BusinessId.Should().Be(businessId);
        return account;
    }

    /// <summary>
    ///     Retrieves available rewards for the specified business in consumer context.
    /// </summary>
    /// <param name="consumerClient">Authorized consumer HTTP client.</param>
    /// <param name="businessId">Target business identifier.</param>
    /// <returns>Rewards list visible to the current consumer user.</returns>
    private static async Task<List<LoyaltyRewardSummary>> GetRewardsForBusinessAsync(HttpClient consumerClient, Guid businessId)
    {
        using var rewardsResponse = await consumerClient.GetAsync($"/api/v1/loyalty/business/{businessId}/rewards");
        rewardsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rewards = await rewardsResponse.Content.ReadFromJsonAsync<List<LoyaltyRewardSummary>>();
        rewards.Should().NotBeNull();
        return rewards!;
    }

    /// <summary>
    ///     Ensures a consumer can redeem by awarding additional points through a valid scan-session accrual cycle.
    ///     This helper intentionally uses the same public API flow as production clients to avoid direct DB coupling.
    /// </summary>
    /// <param name="consumerClient">Authorized consumer HTTP client.</param>
    /// <param name="businessClient">Authorized business HTTP client.</param>
    /// <param name="businessId">Target business identifier shared by both roles.</param>
    /// <param name="minimumExtraPoints">Additional points to award before redemption test.</param>
    private static async Task EnsureSufficientPointsForRedemptionAsync(HttpClient consumerClient, HttpClient businessClient, Guid businessId, int minimumExtraPoints)
    {
        using var prepareResponse = await consumerClient.PostAsJsonAsync("/api/v1/loyalty/scan/prepare", new PrepareScanSessionRequest
        {
            BusinessId = businessId,
            Mode = LoyaltyScanMode.Accrual,
            SelectedRewardTierIds = []
        });

        prepareResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var prepareBody = await prepareResponse.Content.ReadFromJsonAsync<PrepareScanSessionResponse>();
        prepareBody.Should().NotBeNull();

        using var processResponse = await businessClient.PostAsJsonAsync("/api/v1/loyalty/scan/process", new ProcessScanSessionForBusinessRequest
        {
            ScanSessionToken = prepareBody!.ScanSessionToken
        });

        processResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var confirmResponse = await businessClient.PostAsJsonAsync("/api/v1/loyalty/scan/confirm-accrual", new ConfirmAccrualRequest
        {
            ScanSessionToken = prepareBody.ScanSessionToken,
            Points = Math.Max(1, minimumExtraPoints),
            Note = "Preparatory accrual for redemption test readiness."
        });

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmBody = await confirmResponse.Content.ReadFromJsonAsync<ConfirmAccrualResponse>();
        confirmBody.Should().NotBeNull();
        confirmBody!.Success.Should().BeTrue();
    }

    /// <summary>
    ///     Extracts business identifier claim from an access token issued to a business account.
    ///     The token is parsed locally (without validation) for test-routing purposes only.
    /// </summary>
    /// <param name="accessToken">JWT access token string.</param>
    /// <returns>Parsed business identifier from <c>business_id</c> claim.</returns>
    private static Guid ExtractBusinessIdFromAccessToken(string accessToken)
    {
        accessToken.Should().NotBeNullOrWhiteSpace();

        var parts = accessToken.Split('.');
        parts.Length.Should().BeGreaterThanOrEqualTo(2);

        var payload = parts[1]
            .Replace('-', '+', StringComparison.Ordinal)
            .Replace('_', '/', StringComparison.Ordinal);

        var paddedPayload = payload.PadRight(payload.Length + ((4 - payload.Length % 4) % 4), '=');
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(paddedPayload));

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("business_id", out var businessIdProp).Should().BeTrue();

        var businessIdText = businessIdProp.GetString();
        Guid.TryParse(businessIdText, out var businessId).Should().BeTrue();
        return businessId;
    }

    private const string SeedConsumerEmail = "cons1@darwin.de";
    private const string SeedConsumerPassword = "Consumer123!";
    private const string SeedConsumerDeviceId = "it-consumer-device";

    private const string SeedBusinessEmail = "biz1@darwin.de";
    private const string SeedBusinessPassword = "Business123!";
    private const string SeedBusinessDeviceId = "it-business-device";
}
