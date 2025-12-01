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
    /// Darwin.WebApi using the shared <see cref="IApiClient"/> abstraction.
    /// </summary>
    public sealed class LoyaltyService : ILoyaltyService
    {
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
                // Contracts use SelectedRewardTierIds; view models pass generic reward IDs.
                // For now we assume these are reward tier IDs.
                SelectedRewardTierIds = selectedRewardIds ?? Array.Empty<Guid>()
            };

            try
            {
                var response = await _apiClient
                    .PostAsync<PrepareScanSessionRequest, PrepareScanSessionResponse>(
                        "/api/loyalty/scan/prepare",
                        request,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (response is null)
                {
                    return Result<ScanSessionClientModel>.Fail("Empty response from server.");
                }

                // QR should contain an opaque string token; currently we encode the Guid id.
                var token = response.ScanSessionId.ToString("D");

                var model = new ScanSessionClientModel
                {
                    Token = token,
                    ExpiresAtUtc = response.ExpiresAtUtc,
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
                var response = await _apiClient
                    .GetAsync<LoyaltyAccountSummary>(
                        $"/api/loyalty/account/{businessId}",
                        cancellationToken)
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
                    $"Network error while loading account summary: {ex.Message}");
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
                var response = await _apiClient
                    .GetAsync<LoyaltyRewardSummary[]>(
                        $"/api/loyalty/rewards/{businessId}",
                        cancellationToken)
                    .ConfigureAwait(false);

                var rewards = (IReadOnlyList<LoyaltyRewardSummary>?)response
                              ?? Array.Empty<LoyaltyRewardSummary>();

                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(rewards);
            }
            catch (HttpRequestException ex)
            {
                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Fail(
                    $"Network error while loading rewards: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<BusinessScanSessionClientModel>> ProcessScanSessionForBusinessAsync(
            string qrToken,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(qrToken))
            {
                return Result<BusinessScanSessionClientModel>.Fail("QR token is missing.");
            }

            if (!Guid.TryParse(qrToken, out var scanSessionId))
            {
                return Result<BusinessScanSessionClientModel>.Fail("QR token format is invalid.");
            }

            var request = new ProcessScanSessionForBusinessRequest
            {
                ScanSessionId = scanSessionId
            };

            try
            {
                var response = await _apiClient
                    .PostAsync<ProcessScanSessionForBusinessRequest, ProcessScanSessionForBusinessResponse>(
                        "/api/loyalty/scan/process",
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
                    AccountSummary = response.AccountSummary,
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
                return Result<LoyaltyAccountSummary>.Fail("Session token is missing.");
            }

            if (!Guid.TryParse(sessionToken, out var scanSessionId))
            {
                return Result<LoyaltyAccountSummary>.Fail("Session token format is invalid.");
            }

            if (points <= 0)
            {
                return Result<LoyaltyAccountSummary>.Fail("Points must be greater than zero.");
            }

            var request = new ConfirmAccrualRequest
            {
                ScanSessionId = scanSessionId,
                Points = points
            };

            try
            {
                var response = await _apiClient
                    .PostAsync<ConfirmAccrualRequest, ConfirmAccrualResponse>(
                        "/api/loyalty/scan/confirm-accrual",
                        request,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (response is null)
                {
                    return Result<LoyaltyAccountSummary>.Fail("Empty response from server.");
                }

                if (!response.Success)
                {
                    var errorMessage = string.IsNullOrWhiteSpace(response.ErrorMessage)
                        ? "Accrual operation was rejected by the server."
                        : response.ErrorMessage;

                    return Result<LoyaltyAccountSummary>.Fail(errorMessage);
                }

                // Contracts now only return NewBalance; we synthesize a minimal summary.
                var summary = new LoyaltyAccountSummary
                {
                    BusinessId = Guid.Empty,
                    BusinessName = string.Empty,
                    PointsBalance = response.NewBalance,
                    LastAccrualAtUtc = DateTime.UtcNow,
                    NextRewardTitle = null
                };

                return Result<LoyaltyAccountSummary>.Ok(summary);
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
                return Result<LoyaltyAccountSummary>.Fail("Session token is missing.");
            }

            if (!Guid.TryParse(sessionToken, out var scanSessionId))
            {
                return Result<LoyaltyAccountSummary>.Fail("Session token format is invalid.");
            }

            var request = new ConfirmRedemptionRequest
            {
                ScanSessionId = scanSessionId
            };

            try
            {
                var response = await _apiClient
                    .PostAsync<ConfirmRedemptionRequest, ConfirmRedemptionResponse>(
                        "/api/loyalty/scan/confirm-redemption",
                        request,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (response is null)
                {
                    return Result<LoyaltyAccountSummary>.Fail("Empty response from server.");
                }

                if (!response.Success)
                {
                    var errorMessage = string.IsNullOrWhiteSpace(response.ErrorMessage)
                        ? "Redemption operation was rejected by the server."
                        : response.ErrorMessage;

                    return Result<LoyaltyAccountSummary>.Fail(errorMessage);
                }

                // Again, synthesize a minimal summary from NewBalance.
                var summary = new LoyaltyAccountSummary
                {
                    BusinessId = Guid.Empty,
                    BusinessName = string.Empty,
                    PointsBalance = response.NewBalance,
                    LastAccrualAtUtc = null,
                    NextRewardTitle = null
                };

                return Result<LoyaltyAccountSummary>.Ok(summary);
            }
            catch (HttpRequestException ex)
            {
                return Result<LoyaltyAccountSummary>.Fail(
                    $"Network error while confirming redemption: {ex.Message}");
            }
        }
    }
}
