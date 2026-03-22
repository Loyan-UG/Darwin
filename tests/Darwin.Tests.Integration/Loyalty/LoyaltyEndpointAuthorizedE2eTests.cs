using Darwin.Contracts.Common;
using Darwin.Contracts.Loyalty;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;

using Darwin.Tests.Common.TestInfrastructure;
using Darwin.Tests.Integration.Support;
using IdentityFlowTestHelper = Darwin.Tests.Common.TestInfrastructure.IdentityFlowTestHelper;

namespace Darwin.Tests.Integration.Loyalty;

/// <summary>
///     Provides authorized end-to-end integration coverage for the loyalty scan-session flow.
///     These tests exercise real multi-step journeys across consumer and business roles:
///     prepare, process, and confirm for both accrual and redemption modes.
/// </summary>
public sealed class LoyaltyEndpointAuthorizedE2eTests : DeterministicIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    /// <summary>
    ///     Initializes the test fixture with a host configured for Testing environment.
    /// </summary>
    /// <param name="factory">Shared WebApplicationFactory instance.</param>
    public LoyaltyEndpointAuthorizedE2eTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
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
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        using var consumerClient = CreateHttpsClient();
        using var businessClient = CreateHttpsClient();

        var consumerToken = await IdentityFlowTestHelper.LoginExpectSuccessAsync(
            consumerClient, SeedConsumerEmail, SeedConsumerPassword, SeedConsumerDeviceId, cancellationToken);
        var businessToken = await IdentityFlowTestHelper.LoginExpectSuccessAsync(
            businessClient, SeedBusinessEmail, SeedBusinessPassword, SeedBusinessDeviceId, cancellationToken);

        var businessId = ExtractBusinessIdFromAccessToken(businessToken.AccessToken);
        businessId.Should().NotBe(Guid.Empty);

        consumerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", consumerToken.AccessToken);
        businessClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", businessToken.AccessToken);

        // Ensure consumer has an account for the same business used by the business token.
        await JoinBusinessIfNeededAsync(consumerClient, businessId, cancellationToken);
        var accountBefore = await GetAccountForBusinessAsync(consumerClient, businessId, cancellationToken);

        // Step 1: Prepare scan session in accrual mode as consumer.
        using var prepareResponse = await consumerClient.PostAsJsonAsync(
            "/api/v1/loyalty/scan/prepare",
            new PrepareScanSessionRequest
            {
                BusinessId = businessId,
                Mode = LoyaltyScanMode.Accrual,
                SelectedRewardTierIds = []
            },
            cancellationToken: TestContext.Current.CancellationToken);

        prepareResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var prepareBody = await prepareResponse.Content.ReadFromJsonAsync<PrepareScanSessionResponse>(cancellationToken: TestContext.Current.CancellationToken);
        prepareBody.Should().NotBeNull();
        prepareBody!.ScanSessionToken.Should().NotBeNullOrWhiteSpace();
        prepareBody.Mode.Should().Be(LoyaltyScanMode.Accrual);

        // Step 2: Process the same token as business app.
        using var processResponse = await businessClient.PostAsJsonAsync(
            "/api/v1/loyalty/scan/process",
            new ProcessScanSessionForBusinessRequest
            {
                ScanSessionToken = prepareBody.ScanSessionToken
            },
            cancellationToken: TestContext.Current.CancellationToken);

        processResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var processBody = await processResponse.Content.ReadFromJsonAsync<ProcessScanSessionForBusinessResponse>(cancellationToken: TestContext.Current.CancellationToken);
        processBody.Should().NotBeNull();
        processBody!.Mode.Should().Be(LoyaltyScanMode.Accrual);
        processBody.BusinessId.Should().Be(businessId);

        // Step 3: Confirm accrual as business app.
        const int awardedPoints = 7;
        using var confirmResponse = await businessClient.PostAsJsonAsync(
            "/api/v1/loyalty/scan/confirm-accrual",
            new ConfirmAccrualRequest
            {
                ScanSessionToken = prepareBody.ScanSessionToken,
                Points = awardedPoints,
                Note = "Integration accrual confirmation."
            },
            cancellationToken: TestContext.Current.CancellationToken);

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmBody = await confirmResponse.Content.ReadFromJsonAsync<ConfirmAccrualResponse>(cancellationToken: TestContext.Current.CancellationToken);
        confirmBody.Should().NotBeNull();
        confirmBody!.Success.Should().BeTrue();

        // Step 4: Verify updated balance from consumer perspective.
        var accountAfter = await GetAccountForBusinessAsync(consumerClient, businessId, cancellationToken);
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
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        using var consumerClient = CreateHttpsClient();
        using var businessClient = CreateHttpsClient();

        var consumerToken = await IdentityFlowTestHelper.LoginExpectSuccessAsync(
            consumerClient, SeedConsumerEmail, SeedConsumerPassword, SeedConsumerDeviceId, cancellationToken);
        var businessToken = await IdentityFlowTestHelper.LoginExpectSuccessAsync(
            businessClient, SeedBusinessEmail, SeedBusinessPassword, SeedBusinessDeviceId, cancellationToken);

        var businessId = ExtractBusinessIdFromAccessToken(businessToken.AccessToken);
        businessId.Should().NotBe(Guid.Empty);

        consumerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", consumerToken.AccessToken);
        businessClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", businessToken.AccessToken);

        // Ensure consumer account exists for target business.
        await JoinBusinessIfNeededAsync(consumerClient, businessId, cancellationToken);

        // Ensure consumer has enough points to make at least one reward selectable.
        await EnsureSufficientPointsForRedemptionAsync(consumerClient, businessClient, businessId, minimumExtraPoints: 300, cancellationToken);

        var rewards = await GetRewardsForBusinessAsync(consumerClient, businessId, cancellationToken);
        var selectedReward = rewards.FirstOrDefault(r => r.IsSelectable);
        selectedReward.Should().NotBeNull("consumer must have at least one selectable reward after preparatory accrual");

        // Step 1: Prepare redemption scan session as consumer.
        using var prepareResponse = await consumerClient.PostAsJsonAsync(
            "/api/v1/loyalty/scan/prepare",
            new PrepareScanSessionRequest
            {
                BusinessId = businessId,
                Mode = LoyaltyScanMode.Redemption,
                SelectedRewardTierIds = [selectedReward!.LoyaltyRewardTierId]
            },
            cancellationToken: TestContext.Current.CancellationToken);

        prepareResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var prepareBody = await prepareResponse.Content.ReadFromJsonAsync<PrepareScanSessionResponse>(cancellationToken: TestContext.Current.CancellationToken);
        prepareBody.Should().NotBeNull();
        prepareBody!.ScanSessionToken.Should().NotBeNullOrWhiteSpace();
        prepareBody.Mode.Should().Be(LoyaltyScanMode.Redemption);

        // Step 2: Process the redemption token as business app.
        using var processResponse = await businessClient.PostAsJsonAsync(
            "/api/v1/loyalty/scan/process",
            new ProcessScanSessionForBusinessRequest
            {
                ScanSessionToken = prepareBody.ScanSessionToken
            },
            cancellationToken: TestContext.Current.CancellationToken);

        processResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var processBody = await processResponse.Content.ReadFromJsonAsync<ProcessScanSessionForBusinessResponse>(cancellationToken: TestContext.Current.CancellationToken);
        processBody.Should().NotBeNull();
        processBody!.Mode.Should().Be(LoyaltyScanMode.Redemption);
        processBody.BusinessId.Should().Be(businessId);
        processBody.SelectedRewards.Should().NotBeNull();
        processBody.SelectedRewards.Should().NotBeEmpty();

        // Step 3: Confirm redemption as business app.
        using var confirmResponse = await businessClient.PostAsJsonAsync(
            "/api/v1/loyalty/scan/confirm-redemption",
            new ConfirmRedemptionRequest
            {
                ScanSessionToken = prepareBody.ScanSessionToken
            },
            cancellationToken: TestContext.Current.CancellationToken);

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmBody = await confirmResponse.Content.ReadFromJsonAsync<ConfirmRedemptionResponse>(cancellationToken: TestContext.Current.CancellationToken);
        confirmBody.Should().NotBeNull();
        confirmBody!.Success.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that an authenticated member can request promotions via
    /// <c>POST /api/v1/loyalty/my/promotions</c> and receive:
    /// 1) a resolved/applied policy object (including independent frequency/suppression windows),
    /// 2) diagnostics counters required by operations and troubleshooting views.
    ///
    /// Why JSON-shape validation is used:
    /// - This endpoint may evolve transport wrappers (for example envelope/no-envelope transitions).
    /// - The test keeps strict validation on contract fields while remaining resilient to wrapper shape.
    /// </summary>
    [Fact]
    public async Task PostMyPromotions_Should_ReturnAppliedPolicyAndDiagnosticsShape_WhenAuthorizedMember()
    {
        // Arrange
        using var consumerClient = CreateHttpsClient();
        using var businessClient = CreateHttpsClient();

        var consumerToken = await IdentityFlowTestHelper.LoginExpectSuccessAsync(consumerClient, SeedConsumerEmail, SeedConsumerPassword, SeedConsumerDeviceId, TestContext.Current.CancellationToken);

        var businessToken = await IdentityFlowTestHelper.LoginExpectSuccessAsync(businessClient, SeedBusinessEmail, SeedBusinessPassword, SeedBusinessDeviceId, TestContext.Current.CancellationToken);

        var businessId = ExtractBusinessIdFromAccessToken(businessToken.AccessToken);
        businessId.Should().NotBe(Guid.Empty);

        consumerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", consumerToken.AccessToken);

        // Ensure the member is joined to at least one business so promotions can be resolved
        // within a realistic consumer context.
        await JoinBusinessIfNeededAsync(consumerClient, businessId, CancellationToken.None);

        const int requestedMaxCards = 6;
        const int requestedFrequencyWindowMinutes = 30;
        const int requestedSuppressionWindowMinutes = 240;

        var requestJson = $$"""
        {
          "page": 1,
          "pageSize": 20,
          "policy": {
            "enableDeduplication": true,
            "maxCards": {{requestedMaxCards}},
            "frequencyWindowMinutes": {{requestedFrequencyWindowMinutes}},
            "suppressionWindowMinutes": {{requestedSuppressionWindowMinutes}}
          }
        }
        """;

        // Act
        using var response = await consumerClient.PostAsync("/api/v1/loyalty/my/promotions", new StringContent(requestJson, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseJson = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(responseJson);

        var payload = UnwrapApiEnvelopeDataIfPresent(document.RootElement);
        var appliedPolicy = GetRequiredObjectProperty(payload, "appliedPolicy");
        var diagnostics = GetRequiredObjectProperty(payload, "diagnostics");

        // Applied policy assertions: frequency and suppression must remain independent.
        GetRequiredInt32Property(appliedPolicy, "maxCards").Should().Be(requestedMaxCards);
        GetRequiredInt32Property(appliedPolicy, "frequencyWindowMinutes").Should().Be(requestedFrequencyWindowMinutes);
        GetRequiredInt32Property(appliedPolicy, "suppressionWindowMinutes").Should().Be(requestedSuppressionWindowMinutes);

        // Diagnostics counters: required presence + non-negative values + basic coherence.
        var initialCandidates = GetRequiredInt32Property(diagnostics, "initialCandidates");
        var suppressedByFrequency = GetRequiredInt32Property(diagnostics, "suppressedByFrequency");
        var deduplicated = GetRequiredInt32Property(diagnostics, "deduplicated");
        var trimmedByCap = GetRequiredInt32Property(diagnostics, "trimmedByCap");
        var finalCount = GetRequiredInt32Property(diagnostics, "finalCount");

        initialCandidates.Should().BeGreaterThanOrEqualTo(0);
        suppressedByFrequency.Should().BeGreaterThanOrEqualTo(0);
        deduplicated.Should().BeGreaterThanOrEqualTo(0);
        trimmedByCap.Should().BeGreaterThanOrEqualTo(0);
        finalCount.Should().BeGreaterThanOrEqualTo(0);
        finalCount.Should().BeLessThanOrEqualTo(initialCandidates);
    }

    /// <summary>
    /// Verifies coherence between diagnostics counters and the returned promotions collection:
    /// - the returned item count must not exceed requested max-cards policy,
    /// - the returned item count must not exceed diagnostics <c>finalCount</c>,
    /// - when <c>finalCount</c> is zero, the visible collection must also be empty.
    /// 
    /// This test intentionally requests a large page size so policy cap is the dominant limiter.
    /// </summary>
    [Fact]
    public async Task PostMyPromotions_Should_ReturnItemCount_CoherentWithDiagnosticsFinalCount_WhenAuthorizedMember()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        using var consumerClient = CreateHttpsClient();
        using var businessClient = CreateHttpsClient();

        var consumerToken = await IdentityFlowTestHelper.LoginExpectSuccessAsync(
            consumerClient,
            SeedConsumerEmail,
            SeedConsumerPassword,
            SeedConsumerDeviceId,
            cancellationToken);

        var businessToken = await IdentityFlowTestHelper.LoginExpectSuccessAsync(
            businessClient,
            SeedBusinessEmail,
            SeedBusinessPassword,
            SeedBusinessDeviceId,
            cancellationToken);

        var businessId = ExtractBusinessIdFromAccessToken(businessToken.AccessToken);
        businessId.Should().NotBe(Guid.Empty);

        consumerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", consumerToken.AccessToken);

        await JoinBusinessIfNeededAsync(consumerClient, businessId, cancellationToken);

        const int requestedMaxCards = 6;
        const int requestedPageSize = 50;

        var requestJson = $$"""
        {
          "page": 1,
          "pageSize": {{requestedPageSize}},
          "policy": {
            "enableDeduplication": true,
            "maxCards": {{requestedMaxCards}},
            "frequencyWindowMinutes": 30,
            "suppressionWindowMinutes": 240
          }
        }
        """;
        //
        // Act
        using var response = await consumerClient.PostAsync(
            "/api/v1/loyalty/my/promotions",
            new StringContent(requestJson, Encoding.UTF8, "application/json"),
            cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(responseJson);
        var payload = UnwrapApiEnvelopeDataIfPresent(document.RootElement);

        var diagnostics = GetRequiredObjectProperty(payload, "diagnostics");
        var finalCount = GetRequiredInt32Property(diagnostics, "finalCount");

        var returnedItemCount = GetReturnedPromotionItemCount(payload);

        returnedItemCount.Should().BeGreaterThanOrEqualTo(0);
        returnedItemCount.Should().BeLessThanOrEqualTo(requestedMaxCards);
        returnedItemCount.Should().BeLessThanOrEqualTo(finalCount);

        if (finalCount == 0)
        {
            returnedItemCount.Should().Be(0);
        }
    }

    /// <summary>
    /// Verifies that promotions endpoint honors a strict max-card policy requested by the client.
    /// This protects mobile rendering assumptions where card count must stay bounded by applied policy.
    /// </summary>
    [Fact]
    public async Task PostMyPromotions_Should_HonorRequestedMaxCardsPolicy_WhenAuthorizedMember()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        using var consumerClient = CreateHttpsClient();
        using var businessClient = CreateHttpsClient();

        var consumerToken = await IdentityFlowTestHelper.LoginExpectSuccessAsync(
            consumerClient,
            SeedConsumerEmail,
            SeedConsumerPassword,
            SeedConsumerDeviceId,
            cancellationToken);

        var businessToken = await IdentityFlowTestHelper.LoginExpectSuccessAsync(
            businessClient,
            SeedBusinessEmail,
            SeedBusinessPassword,
            SeedBusinessDeviceId,
            cancellationToken);

        var businessId = ExtractBusinessIdFromAccessToken(businessToken.AccessToken);
        businessId.Should().NotBe(Guid.Empty);

        consumerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", consumerToken.AccessToken);

        await JoinBusinessIfNeededAsync(consumerClient, businessId, cancellationToken);

        const int requestedMaxCards = 1;

        var requestJson = $$"""
        {
          "page": 1,
          "pageSize": 50,
          "policy": {
            "enableDeduplication": true,
            "maxCards": {{requestedMaxCards}},
            "frequencyWindowMinutes": 30,
            "suppressionWindowMinutes": 240
          }
        }
        """;

        // Act
        using var response = await consumerClient.PostAsync(
            "/api/v1/loyalty/my/promotions",
            new StringContent(requestJson, Encoding.UTF8, "application/json"),
            cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var payload = UnwrapApiEnvelopeDataIfPresent(doc.RootElement);

        var appliedPolicy = GetRequiredObjectProperty(payload, "appliedPolicy");
        var diagnostics = GetRequiredObjectProperty(payload, "diagnostics");

        var resolvedMaxCards = GetRequiredInt32Property(appliedPolicy, "maxCards");
        var finalCount = GetRequiredInt32Property(diagnostics, "finalCount");
        var visibleCount = GetReturnedPromotionItemCount(payload);

        resolvedMaxCards.Should().Be(requestedMaxCards);
        visibleCount.Should().BeLessThanOrEqualTo(requestedMaxCards);
        visibleCount.Should().BeLessThanOrEqualTo(finalCount);
    }

    /// <summary>
    /// Verifies that applied policy keeps frequency and suppression windows as distinct values.
    /// This guards against accidental field coupling in API responses used by diagnostics UI.
    /// </summary>
    [Fact]
    public async Task PostMyPromotions_Should_ExposeDistinctFrequencyAndSuppressionValues_WhenBothProvided()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        using var consumerClient = CreateHttpsClient();
        using var businessClient = CreateHttpsClient();

        var consumerToken = await IdentityFlowTestHelper.LoginExpectSuccessAsync(
            consumerClient,
            SeedConsumerEmail,
            SeedConsumerPassword,
            SeedConsumerDeviceId,
            cancellationToken);

        var businessToken = await IdentityFlowTestHelper.LoginExpectSuccessAsync(
            businessClient,
            SeedBusinessEmail,
            SeedBusinessPassword,
            SeedBusinessDeviceId,
            cancellationToken);

        var businessId = ExtractBusinessIdFromAccessToken(businessToken.AccessToken);
        businessId.Should().NotBe(Guid.Empty);

        consumerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", consumerToken.AccessToken);

        await JoinBusinessIfNeededAsync(consumerClient, businessId, cancellationToken);

        const int requestedFrequencyWindowMinutes = 15;
        const int requestedSuppressionWindowMinutes = 360;

        var requestJson = $$"""
        {
          "page": 1,
          "pageSize": 20,
          "policy": {
            "enableDeduplication": true,
            "maxCards": 6,
            "frequencyWindowMinutes": {{requestedFrequencyWindowMinutes}},
            "suppressionWindowMinutes": {{requestedSuppressionWindowMinutes}}
          }
        }
        """;

        // Act
        using var response = await consumerClient.PostAsync(
            "/api/v1/loyalty/my/promotions",
            new StringContent(requestJson, Encoding.UTF8, "application/json"),
            cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var payload = UnwrapApiEnvelopeDataIfPresent(doc.RootElement);
        var appliedPolicy = GetRequiredObjectProperty(payload, "appliedPolicy");

        var frequency = GetRequiredInt32Property(appliedPolicy, "frequencyWindowMinutes");
        var suppression = GetRequiredInt32Property(appliedPolicy, "suppressionWindowMinutes");

        frequency.Should().Be(requestedFrequencyWindowMinutes);
        suppression.Should().Be(requestedSuppressionWindowMinutes);
        frequency.Should().NotBe(suppression);
    }

    /// <summary>
    ///     Ensures a consumer loyalty account exists for the target business.
    ///     The endpoint is idempotent and returns existing account when already joined.
    /// </summary>
    /// <param name="consumerClient">Authorized consumer HTTP client.</param>
    /// <param name="businessId">Target business identifier.</param>
    private static async Task JoinBusinessIfNeededAsync(HttpClient consumerClient, Guid businessId, CancellationToken cancellationToken)
    {
        using var joinResponse = await consumerClient.PostAsJsonAsync(
            $"/api/v1/loyalty/account/{businessId}/join",
            new JoinLoyaltyRequest
            {
                BusinessLocationId = null
            },
            cancellationToken);

        joinResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var joinBody = await joinResponse.Content.ReadFromJsonAsync<LoyaltyAccountSummary>(cancellationToken);
        joinBody.Should().NotBeNull();
        joinBody!.BusinessId.Should().Be(businessId);
    }

    /// <summary>
    ///     Reads the current consumer loyalty account for a business.
    /// </summary>
    /// <param name="consumerClient">Authorized consumer HTTP client.</param>
    /// <param name="businessId">Target business identifier.</param>
    /// <returns>Current account summary for the specified business.</returns>
    private static async Task<LoyaltyAccountSummary> GetAccountForBusinessAsync(HttpClient consumerClient, Guid businessId, CancellationToken cancellationToken)
    {
        using var accountResponse = await consumerClient.GetAsync($"/api/v1/loyalty/account/{businessId}", cancellationToken);
        accountResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var account = await accountResponse.Content.ReadFromJsonAsync<LoyaltyAccountSummary>(cancellationToken);
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
    private static async Task<List<LoyaltyRewardSummary>> GetRewardsForBusinessAsync(HttpClient consumerClient, Guid businessId, CancellationToken cancellationToken)
    {
        using var rewardsResponse = await consumerClient.GetAsync($"/api/v1/loyalty/business/{businessId}/rewards", cancellationToken);
        rewardsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rewards = await rewardsResponse.Content.ReadFromJsonAsync<List<LoyaltyRewardSummary>>(cancellationToken);
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
    /// <param name="cancellationToken"></param>
    private static async Task EnsureSufficientPointsForRedemptionAsync(
        HttpClient consumerClient,
        HttpClient businessClient,
        Guid businessId,
        int minimumExtraPoints,
        CancellationToken cancellationToken)
    {
        using var prepareResponse = await consumerClient.PostAsJsonAsync(
            "/api/v1/loyalty/scan/prepare",
            new PrepareScanSessionRequest
            {
                BusinessId = businessId,
                Mode = LoyaltyScanMode.Accrual,
                SelectedRewardTierIds = []
            },
            cancellationToken: TestContext.Current.CancellationToken);

        prepareResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var prepareBody = await prepareResponse.Content.ReadFromJsonAsync<PrepareScanSessionResponse>(cancellationToken);
        prepareBody.Should().NotBeNull();

        using var processResponse = await businessClient.PostAsJsonAsync(
            "/api/v1/loyalty/scan/process",
            new ProcessScanSessionForBusinessRequest
            {
                ScanSessionToken = prepareBody!.ScanSessionToken
            },
            cancellationToken: TestContext.Current.CancellationToken);

        processResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var confirmResponse = await businessClient.PostAsJsonAsync(
            "/api/v1/loyalty/scan/confirm-accrual",
            new ConfirmAccrualRequest
            {
                ScanSessionToken = prepareBody.ScanSessionToken,
                Points = Math.Max(1, minimumExtraPoints),
                Note = "Preparatory accrual for redemption test readiness."
            },
            cancellationToken: TestContext.Current.CancellationToken);

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmBody = await confirmResponse.Content.ReadFromJsonAsync<ConfirmAccrualResponse>(cancellationToken);
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

        var json = DecodeBase64UrlPayload(parts[1]);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("business_id", out var businessIdProp).Should().BeTrue();

        var businessIdText = businessIdProp.GetString();
        Guid.TryParse(businessIdText, out var businessId).Should().BeTrue();
        return businessId;
    }

    /// <summary>
    ///     Decodes a Base64Url-encoded JWT payload segment into its JSON text without relying on
    ///     string replacement overloads that can vary across target frameworks and analyzer contexts.
    /// </summary>
    /// <param name="payloadSegment">Middle JWT segment encoded with Base64Url semantics.</param>
    /// <returns>Decoded JSON payload text.</returns>
    private static string DecodeBase64UrlPayload(string payloadSegment)
    {
        payloadSegment.Should().NotBeNullOrWhiteSpace();

        var payloadBytes = WebEncoders.Base64UrlDecode(payloadSegment);
        return System.Text.Encoding.UTF8.GetString(payloadBytes);
    }

    private const string SeedConsumerEmail = "cons1@darwin.de";
    private const string SeedConsumerPassword = "Consumer123!";
    private const string SeedConsumerDeviceId = "it-consumer-device";

    private const string SeedBusinessEmail = "biz1@darwin.de";
    private const string SeedBusinessPassword = "Business123!";
    private const string SeedBusinessDeviceId = "it-business-device";

    /// <summary>
    /// Returns the envelope <c>data</c> node when the response uses <see cref="ApiEnvelope{T}"/>,
    /// otherwise returns the root JSON object.
    /// </summary>
    private static JsonElement UnwrapApiEnvelopeDataIfPresent(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object &&
            TryGetPropertyIgnoreCase(root, "data", out var dataElement))
        {
            return dataElement;
        }

        return root;
    }

    /// <summary>
    /// Reads a required object property using case-insensitive lookup.
    /// </summary>
    private static JsonElement GetRequiredObjectProperty(JsonElement owner, string propertyName)
    {
        TryGetPropertyIgnoreCase(owner, propertyName, out var value)
            .Should().BeTrue($"property '{propertyName}' must exist in promotions response payload.");

        value.ValueKind.Should().Be(JsonValueKind.Object, $"property '{propertyName}' must be a JSON object.");
        return value;
    }

    /// <summary>
    /// Reads a required Int32 property using case-insensitive lookup.
    /// </summary>
    private static int GetRequiredInt32Property(JsonElement owner, string propertyName)
    {
        TryGetPropertyIgnoreCase(owner, propertyName, out var value)
            .Should().BeTrue($"property '{propertyName}' must exist in promotions response payload.");

        value.ValueKind.Should().Be(JsonValueKind.Number, $"property '{propertyName}' must be numeric.");
        value.TryGetInt32(out var intValue).Should().BeTrue($"property '{propertyName}' must fit Int32 range.");
        return intValue;
    }

    /// <summary>
    /// Performs case-insensitive JSON property lookup for object elements.
    /// </summary>
    private static bool TryGetPropertyIgnoreCase(JsonElement owner, string propertyName, out JsonElement value)
    {
        if (owner.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in owner.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Resolves the returned promotions collection length from known contract collection node names.
    /// The helper is tolerant to naming variants while still enforcing presence of a concrete array node.
    /// </summary>
    private static int GetReturnedPromotionItemCount(JsonElement payload)
    {
        var candidateNames = new[] { "items", "promotions", "cards", "results" };

        foreach (var candidateName in candidateNames)
        {
            if (TryGetPropertyIgnoreCase(payload, candidateName, out var arrayElement))
            {
                arrayElement.ValueKind.Should().Be(
                    JsonValueKind.Array,
                    $"property '{candidateName}' must be an array when present in promotions payload.");
                return arrayElement.GetArrayLength();
            }
        }

        throw new InvalidOperationException(
            "Promotions response payload does not expose a known collection node (items/promotions/cards/results).");
    }
}
