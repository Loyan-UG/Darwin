using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Models.Loyalty;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Services.Loyalty
{
    /// <summary>
    /// Default implementation of <see cref="ILoyaltyService"/> that talks to
    /// the Darwin WebApi using the shared <see cref="IApiClient"/> abstraction.
    /// All network and serialization concerns are handled by the API client;
    /// this class focuses on mapping Contracts to mobile-friendly client models.
    /// </summary>
    public sealed class LoyaltyService : ILoyaltyService
    {
        private const string PrepareScanSessionRoute = "/api/loyalty/scan/prepare";
        private const string ProcessScanSessionForBusinessRoute = "/api/loyalty/scan/process";
        private const string ConfirmAccrualRoute = "/api/loyalty/scan/confirm-accrual";
        private const string ConfirmRedemptionRoute = "/api/loyalty/scan/confirm-redemption";
        private const string GetAccountSummaryRouteTemplate = "/api/loyalty/account/summary/{0}";
        private const string GetAvailableRewardsRouteTemplate = "/api/loyalty/rewards/available/{0}";

        private readonly IApiClient _apiClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoyaltyService"/> class.
        /// </summary>
        /// <param name="apiClient">
        /// Typed HTTP client configured with base URL, JSON options and retry policy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="apiClient"/> is <c>null</c>.
        /// </exception>
        public LoyaltyService(IApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        /// <inheritdoc />
        public async Task<Result<ScanSessionClientModel>> PrepareScanSessionAsync(
            Guid businessId,
            LoyaltyScanMode mode,
            IReadOnlyCollection<Guid>? selectedRewardIds,
            CancellationToken cancellationToken)
        {
            if (businessId == Guid.Empty)
            {
                return Result<ScanSessionClientModel>.Fail("Invalid business identifier.");
            }

            var request = new PrepareScanSessionRequest
            {
                BusinessId = businessId,
                Mode = mode,
                // Contract property is SelectedRewardTierIds.
                SelectedRewardTierIds = selectedRewardIds is null
                    ? Array.Empty<Guid>()
                    : new List<Guid>(selectedRewardIds)
            };

            try
            {
                var response = await _apiClient
                    .PostAsync<PrepareScanSessionRequest, PrepareScanSessionResponse>(
                        PrepareScanSessionRoute,
                        request,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (response is null)
                {
                    return Result<ScanSessionClientModel>.Fail("Empty response from server.");
                }

                // Token-first: QR payload is the opaque ScanSessionToken string.
                // Never attempt to parse it into a Guid; it is not guaranteed to be one.
                if (string.IsNullOrWhiteSpace(response.ScanSessionToken))
                {
                    return Result<ScanSessionClientModel>.Fail("Server did not return a valid QR token.");
                }

                var model = new ScanSessionClientModel
                {
                    Token = response.ScanSessionToken,
                    ExpiresAtUtc = new DateTimeOffset(response.ExpiresAtUtc, TimeSpan.Zero),
                    Mode = response.Mode,
                    SelectedRewards = response.SelectedRewards ?? Array.Empty<LoyaltyRewardSummary>()
                };

                return Result<ScanSessionClientModel>.Ok(model);
            }
            catch (HttpRequestException ex)
            {
                return Result<ScanSessionClientModel>.Fail(
                    $"Network error while preparing scan session: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<LoyaltyAccountSummary>> GetAccountSummaryAsync(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            if (businessId == Guid.Empty)
            {
                return Result<LoyaltyAccountSummary>.Fail("Invalid business identifier.");
            }

            try
            {
                var route = string.Format(GetAccountSummaryRouteTemplate, businessId);
                var response = await _apiClient
                    .GetAsync<LoyaltyAccountSummary>(route, cancellationToken)
                    .ConfigureAwait(false);

                if (response is null)
                {
                    return Result<LoyaltyAccountSummary>.Fail("Empty response from server.");
                }

                return Result<LoyaltyAccountSummary>.Ok(response);
            }
            catch (HttpRequestException ex)
            {
                return Result<LoyaltyAccountSummary>.Fail(
                    $"Network error while retrieving account summary: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<LoyaltyRewardSummary>>> GetAvailableRewardsAsync(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            if (businessId == Guid.Empty)
            {
                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Fail("Invalid business identifier.");
            }

            try
            {
                var route = string.Format(GetAvailableRewardsRouteTemplate, businessId);
                var response = await _apiClient
                    .GetAsync<IReadOnlyList<LoyaltyRewardSummary>>(route, cancellationToken)
                    .ConfigureAwait(false);

                if (response is null)
                {
                    return Result<IReadOnlyList<LoyaltyRewardSummary>>.Fail("Empty response from server.");
                }

                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(response);
            }
            catch (HttpRequestException ex)
            {
                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Fail(
                    $"Network error while retrieving rewards: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<BusinessScanSessionClientModel>> ProcessScanSessionForBusinessAsync(
            string qrToken,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(qrToken))
            {
                return Result<BusinessScanSessionClientModel>.Fail("QR token is required.");
            }

            // Token-first: pass the scanned token as-is to the server.
            var request = new ProcessScanSessionForBusinessRequest
            {
                ScanSessionToken = qrToken
            };

            try
            {
                var response = await _apiClient
                    .PostAsync<ProcessScanSessionForBusinessRequest, ProcessScanSessionForBusinessResponse>(
                        ProcessScanSessionForBusinessRoute,
                        request,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (response is null)
                {
                    return Result<BusinessScanSessionClientModel>.Fail("Empty response from server.");
                }

                var model = new BusinessScanSessionClientModel
                {
                    Token = qrToken,
                    Mode = response.Mode,
                    BusinessId = response.BusinessId,
                    BusinessLocationId = response.BusinessLocationId,
                    AccountSummary = response.AccountSummary ?? new BusinessLoyaltyAccountSummary(),
                    CustomerDisplayName = response.CustomerDisplayName,
                    SelectedRewards = response.SelectedRewards ?? Array.Empty<LoyaltyRewardSummary>(),
                    CanConfirmAccrual = response.AllowedActions.HasFlag(LoyaltyScanAllowedActions.CanConfirmAccrual),
                    CanConfirmRedemption = response.AllowedActions.HasFlag(LoyaltyScanAllowedActions.CanConfirmRedemption)
                };

                return Result<BusinessScanSessionClientModel>.Ok(model);
            }
            catch (HttpRequestException ex)
            {
                return Result<BusinessScanSessionClientModel>.Fail(
                    $"Network error while processing scan: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<LoyaltyAccountSummary>> ConfirmAccrualAsync(
            string sessionToken,
            int points,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                return Result<LoyaltyAccountSummary>.Fail("Session token is required.");
            }

            if (points <= 0)
            {
                return Result<LoyaltyAccountSummary>.Fail("Points must be greater than zero.");
            }

            // Token-first: never parse the token. It is an opaque string and may not be a Guid.
            var request = new ConfirmAccrualRequest
            {
                ScanSessionToken = sessionToken,
                Points = points,
                Note = null
            };

            try
            {
                var response = await _apiClient
                    .PostAsync<ConfirmAccrualRequest, ConfirmAccrualResponse>(
                        ConfirmAccrualRoute,
                        request,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (response is null)
                {
                    return Result<LoyaltyAccountSummary>.Fail("Empty response from server.");
                }

                if (!response.Success)
                {
                    var message = !string.IsNullOrWhiteSpace(response.ErrorMessage)
                        ? response.ErrorMessage
                        : "Accrual could not be confirmed.";

                    if (!string.IsNullOrWhiteSpace(response.ErrorCode))
                    {
                        message = $"{message} (code: {response.ErrorCode})";
                    }

                    return Result<LoyaltyAccountSummary>.Fail(message);
                }

                // Prefer server-provided snapshot; otherwise fall back to NewBalance.
                if (response.UpdatedAccount is not null)
                {
                    return Result<LoyaltyAccountSummary>.Ok(response.UpdatedAccount);
                }

                var synthesized = new LoyaltyAccountSummary
                {
                    BusinessId = Guid.Empty,
                    BusinessName = string.Empty,
                    PointsBalance = response.NewBalance ?? 0,
                    LastAccrualAtUtc = DateTime.UtcNow,
                    NextRewardTitle = null
                };

                return Result<LoyaltyAccountSummary>.Ok(synthesized);
            }
            catch (HttpRequestException ex)
            {
                return Result<LoyaltyAccountSummary>.Fail(
                    $"Network error while confirming accrual: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<LoyaltyAccountSummary>> ConfirmRedemptionAsync(
            string sessionToken,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                return Result<LoyaltyAccountSummary>.Fail("Session token is required.");
            }

            var request = new ConfirmRedemptionRequest
            {
                ScanSessionToken = sessionToken
            };

            try
            {
                var response = await _apiClient
                    .PostAsync<ConfirmRedemptionRequest, ConfirmRedemptionResponse>(
                        ConfirmRedemptionRoute,
                        request,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (response is null)
                {
                    return Result<LoyaltyAccountSummary>.Fail("Empty response from server.");
                }

                if (!response.Success)
                {
                    var message = !string.IsNullOrWhiteSpace(response.ErrorMessage)
                        ? response.ErrorMessage
                        : "Redemption could not be confirmed.";

                    if (!string.IsNullOrWhiteSpace(response.ErrorCode))
                    {
                        message = $"{message} (code: {response.ErrorCode})";
                    }

                    return Result<LoyaltyAccountSummary>.Fail(message);
                }

                if (response.UpdatedAccount is not null)
                {
                    return Result<LoyaltyAccountSummary>.Ok(response.UpdatedAccount);
                }

                var synthesized = new LoyaltyAccountSummary
                {
                    BusinessId = Guid.Empty,
                    BusinessName = string.Empty,
                    PointsBalance = response.NewBalance ?? 0,
                    LastAccrualAtUtc = DateTime.UtcNow,
                    NextRewardTitle = null
                };

                return Result<LoyaltyAccountSummary>.Ok(synthesized);
            }
            catch (HttpRequestException ex)
            {
                return Result<LoyaltyAccountSummary>.Fail(
                    $"Network error while confirming redemption: {ex.Message}");
            }
        }
    }
}
