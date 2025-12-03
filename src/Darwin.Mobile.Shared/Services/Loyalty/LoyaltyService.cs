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
                // Contract property is SelectedRewardTierIds (IReadOnlyList<Guid>).
                // Map null to an empty array to keep things non-nullable and predictable.
                SelectedRewardTierIds = selectedRewardIds is null
                    ? Array.Empty<Guid>()
                    : new List<Guid>(selectedRewardIds)
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

                // PrepareScanSessionResponse exposes ScanSessionId (Guid). The mobile client
                // still works with a string token for QR rendering, so we convert the Guid
                // to a canonical string representation. Using "D" keeps hyphens and is easy
                // to parse back on the server.
                var token = response.ScanSessionId == Guid.Empty
                    ? string.Empty
                    : response.ScanSessionId.ToString("D");

                var model = new ScanSessionClientModel
                {
                    Token = token,
                    // Contract uses DateTime (UTC); client model uses DateTimeOffset?.
                    // We normalize to UTC offset (zero). If the server later uses
                    // DateTimeOffset, this mapping can be simplified.
                    ExpiresAtUtc = new DateTimeOffset(response.ExpiresAtUtc, TimeSpan.Zero),
                    Mode = response.Mode,
                    SelectedRewards = response.SelectedRewards ?? Array.Empty<LoyaltyRewardSummary>()
                };

                if (string.IsNullOrWhiteSpace(model.Token))
                {
                    return Result<ScanSessionClientModel>.Fail("Server did not return a valid QR token.");
                }

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
                // For now we assume the WebApi exposes a simple listing endpoint per business.
                // If business-specific filters are added later, this call can be extended
                // without breaking the ILoyaltyService abstraction.
                var response = await _apiClient
                    .GetAsync<IReadOnlyList<LoyaltyRewardSummary>>(
                        $"/api/loyalty/rewards/{businessId}",
                        cancellationToken)
                    .ConfigureAwait(false);

                var rewards = response ?? Array.Empty<LoyaltyRewardSummary>();
                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(rewards);
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

            // The QR token is the string representation of the ScanSessionId (Guid)
            // encoded by the consumer app. We must be able to parse it back to Guid
            // to call the WebApi contract, which expects ScanSessionId.
            if (!Guid.TryParse(qrToken, out var scanSessionId))
            {
                return Result<BusinessScanSessionClientModel>.Fail("QR token is invalid.");
            }

            // The QR token is the opaque representation of the scan session identifier.
            // The contract expects the same token in the request; the server will
            // resolve it to the underlying scan session entity.
            var request = new ProcessScanSessionForBusinessRequest
            {
                ScanSessionId = scanSessionId
            };

            try
            {
                var response = await _apiClient
                    .PostAsync<ProcessScanSessionForBusinessRequest, ProcessScanSessionForBusinessResponse>(
                        "/api/loyalty/scan/process-business",
                        request,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (response is null)
                {
                    return Result<BusinessScanSessionClientModel>.Fail("Empty response from server.");
                }

                var model = new BusinessScanSessionClientModel
                {
                    // The business app still works with the opaque string token.
                    Token = qrToken,
                    Mode = response.Mode,
                    BusinessId = response.BusinessId,
                    // Contract exposes LoyaltyAccount; map it directly.
                    AccountSummary = response.LoyaltyAccount,
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

            // The session token is the same representation used for the QR code:
            // a Guid serialized as string. The contract expects ScanSessionId (Guid),
            // so we must parse it.
            if (!Guid.TryParse(sessionToken, out var scanSessionId))
            {
                return Result<LoyaltyAccountSummary>.Fail("Session token is invalid.");
            }

            var request = new ConfirmAccrualRequest
            {
                ScanSessionId = scanSessionId,
                Points = points,
                // Note can be extended later (e.g., cashier id, POS reference, etc.).
                Note = null
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
                    var message = !string.IsNullOrWhiteSpace(response.ErrorMessage)
                        ? response.ErrorMessage
                        : "Accrual could not be confirmed.";

                    // Optionally append error code for diagnostics.
                    if (!string.IsNullOrWhiteSpace(response.ErrorCode))
                    {
                        message = $"{message} (code: {response.ErrorCode})";
                    }

                    return Result<LoyaltyAccountSummary>.Fail(message);
                }

                // Prefer the richer UpdatedAccount snapshot when provided by the server.
                LoyaltyAccountSummary summary;

                if (response.UpdatedAccount is not null)
                {
                    summary = response.UpdatedAccount;
                }
                else
                {
                    // For backwards compatibility with servers that only populate NewBalance,
                    // synthesize a minimal summary. BusinessId and BusinessName are unknown
                    // at this point, so we keep them neutral. View models that only care
                    // about the numeric balance will still work correctly.
                    summary = new LoyaltyAccountSummary
                    {
                        BusinessId = Guid.Empty,
                        BusinessName = string.Empty,
                        PointsBalance = response.NewBalance ?? 0,
                        LastAccrualAtUtc = DateTime.UtcNow,
                        NextRewardTitle = null
                    };
                }

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
                return Result<LoyaltyAccountSummary>.Fail("Session token is required.");
            }

            if (!Guid.TryParse(sessionToken, out var scanSessionId))
            {
                return Result<LoyaltyAccountSummary>.Fail("Session token is invalid.");
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
                    var message = !string.IsNullOrWhiteSpace(response.ErrorMessage)
                        ? response.ErrorMessage
                        : "Redemption could not be confirmed.";

                    if (!string.IsNullOrWhiteSpace(response.ErrorCode))
                    {
                        message = $"{message} (code: {response.ErrorCode})";
                    }

                    return Result<LoyaltyAccountSummary>.Fail(message);
                }

                // Again, prefer the full UpdatedAccount snapshot when available.
                LoyaltyAccountSummary summary;

                if (response.UpdatedAccount is not null)
                {
                    summary = response.UpdatedAccount;
                }
                else
                {
                    summary = new LoyaltyAccountSummary
                    {
                        BusinessId = Guid.Empty,
                        BusinessName = string.Empty,
                        PointsBalance = response.NewBalance ?? 0,
                        LastAccrualAtUtc = DateTime.UtcNow,
                        NextRewardTitle = null
                    };
                }

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
