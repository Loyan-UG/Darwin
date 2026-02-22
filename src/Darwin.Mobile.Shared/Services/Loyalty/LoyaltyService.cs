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
    /// High-level loyalty facade used by the mobile apps.
    /// Responsibilities:
    /// - Prepare scan sessions (consumer) and process scanned tokens (business).
    /// - Confirm accrual and redemption.
    /// - Read models for accounts, history, rewards and "my places".
    /// Contract-first: consumes Darwin.WebApi endpoints using Darwin.Contracts.
    /// 
    /// Rationale / Pitfalls:
    /// - ApiClient returns Result<T></T> instead of throwing on normal HTTP errors.
    /// - We must honor cancellation (OperationCanceledException) and not swallow it.
    /// - Use conservative exception handling to avoid crashing UI on unexpected parse errors.
    /// </summary>
    public sealed class LoyaltyService : ILoyaltyService
    {
        private readonly IApiClient _apiClient;

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
                SelectedRewardTierIds = selectedRewardIds is null
                    ? Array.Empty<Guid>()
                    : new List<Guid>(selectedRewardIds)
            };

            try
            {
                var response = await _apiClient
                    .PostResultAsync<PrepareScanSessionRequest, PrepareScanSessionResponse>(
                        ApiRoutes.Loyalty.PrepareScanSession,
                        request,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (!response.Succeeded || response.Value is null)
                {
                    return Result<ScanSessionClientModel>.Fail(response.Error ?? "Request failed.");
                }

                var payload = response.Value;

                if (string.IsNullOrWhiteSpace(payload.ScanSessionToken))
                {
                    return Result<ScanSessionClientModel>.Fail("Server did not return a valid QR token.");
                }

                var model = new ScanSessionClientModel
                {
                    Token = payload.ScanSessionToken,
                    ExpiresAtUtc = new DateTimeOffset(payload.ExpiresAtUtc, TimeSpan.Zero),
                    Mode = payload.Mode,
                    SelectedRewards = payload.SelectedRewards ?? Array.Empty<LoyaltyRewardSummary>()
                };

                return Result<ScanSessionClientModel>.Ok(model);
            }
            catch (OperationCanceledException)
            {
                // Propagate cancellation to caller so callers can respect timeout/token semantics.
                throw;
            }
            catch (Exception ex)
            {
                // Any other error is treated as a network/transport/parsing failure from the client's perspective.
                return Result<ScanSessionClientModel>.Fail($"Network error while preparing scan session: {ex.Message}");
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
                    .GetResultAsync<LoyaltyAccountSummary>(ApiRoutes.Loyalty.GetAccountForBusiness(businessId), cancellationToken)
                    .ConfigureAwait(false);

                if (!response.Succeeded || response.Value is null)
                    return Result<LoyaltyAccountSummary>.Fail(response.Error ?? "Request failed.");

                return Result<LoyaltyAccountSummary>.Ok(response.Value);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<LoyaltyAccountSummary>.Fail($"Network error while retrieving account summary: {ex.Message}");
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
                    .GetResultAsync<IReadOnlyList<LoyaltyRewardSummary>>(ApiRoutes.Loyalty.GetRewardsForBusiness(businessId), cancellationToken)
                    .ConfigureAwait(false);

                if (!response.Succeeded || response.Value is null)
                {
                    return Result<IReadOnlyList<LoyaltyRewardSummary>>.Fail(response.Error ?? "Request failed.");
                }

                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(response.Value);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Fail($"Network error while retrieving rewards: {ex.Message}");
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

            var request = new ProcessScanSessionForBusinessRequest
            {
                ScanSessionToken = qrToken
            };

            try
            {
                var response = await _apiClient
                    .PostResultAsync<ProcessScanSessionForBusinessRequest, ProcessScanSessionForBusinessResponse>(
                        ApiRoutes.Loyalty.ProcessScanSessionForBusiness,
                        request,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (!response.Succeeded || response.Value is null)
                {
                    return Result<BusinessScanSessionClientModel>.Fail(response.Error ?? "Request failed.");
                }

                var payload = response.Value;

                var model = new BusinessScanSessionClientModel
                {
                    Token = qrToken,
                    Mode = payload.Mode,
                    BusinessId = payload.BusinessId,
                    BusinessLocationId = payload.BusinessLocationId,
                    AccountSummary = payload.AccountSummary ?? new BusinessLoyaltyAccountSummary(),
                    CustomerDisplayName = payload.CustomerDisplayName,
                    SelectedRewards = payload.SelectedRewards ?? Array.Empty<LoyaltyRewardSummary>(),
                    CanConfirmAccrual = payload.AllowedActions.HasFlag(LoyaltyScanAllowedActions.CanConfirmAccrual),
                    CanConfirmRedemption = payload.AllowedActions.HasFlag(LoyaltyScanAllowedActions.CanConfirmRedemption)
                };

                return Result<BusinessScanSessionClientModel>.Ok(model);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<BusinessScanSessionClientModel>.Fail($"Network error while processing scan: {ex.Message}");
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

            var request = new ConfirmAccrualRequest
            {
                ScanSessionToken = sessionToken,
                Points = points,
                Note = null
            };

            try
            {
                var response = await _apiClient
                    .PostResultAsync<ConfirmAccrualRequest, ConfirmAccrualResponse>(
                        ApiRoutes.Loyalty.ConfirmAccrual,
                        request,
                        cancellationToken).ConfigureAwait(false);

                if (!response.Succeeded || response.Value is null)
                    return Result<LoyaltyAccountSummary>.Fail(response.Error ?? "Request failed.");

                var payload = response.Value;

                if (!payload.Success)
                {
                    var message = !string.IsNullOrWhiteSpace(payload.ErrorMessage)
                        ? payload.ErrorMessage
                        : "Accrual could not be confirmed.";

                    if (!string.IsNullOrWhiteSpace(payload.ErrorCode))
                        message = $"{message} (code: {payload.ErrorCode})";

                    return Result<LoyaltyAccountSummary>.Fail(message);
                }

                if (payload.UpdatedAccount is not null)
                    return Result<LoyaltyAccountSummary>.Ok(payload.UpdatedAccount);

                return Result<LoyaltyAccountSummary>.Ok(new LoyaltyAccountSummary
                {
                    BusinessId = Guid.Empty,
                    BusinessName = string.Empty,
                    PointsBalance = payload.NewBalance ?? 0,
                    LastAccrualAtUtc = DateTime.UtcNow
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<LoyaltyAccountSummary>.Fail($"Network error while confirming accrual: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<LoyaltyAccountSummary>> ConfirmRedemptionAsync(string sessionToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                return Result<LoyaltyAccountSummary>.Fail("Session token is required.");

            var request = new ConfirmRedemptionRequest { ScanSessionToken = sessionToken };

            try
            {
                var response = await _apiClient
                    .PostResultAsync<ConfirmRedemptionRequest, ConfirmRedemptionResponse>(
                        ApiRoutes.Loyalty.ConfirmRedemption,
                        request,
                        cancellationToken).ConfigureAwait(false);

                if (!response.Succeeded || response.Value is null)
                    return Result<LoyaltyAccountSummary>.Fail(response.Error ?? "Request failed.");

                var payload = response.Value;

                if (!payload.Success)
                {
                    var message = !string.IsNullOrWhiteSpace(payload.ErrorMessage) ? payload.ErrorMessage : "Redemption could not be confirmed.";
                    if (!string.IsNullOrWhiteSpace(payload.ErrorCode))
                        message = $"{message} (code: {payload.ErrorCode})";
                    return Result<LoyaltyAccountSummary>.Fail(message);
                }

                if (payload.UpdatedAccount is not null)
                    return Result<LoyaltyAccountSummary>.Ok(payload.UpdatedAccount);

                return Result<LoyaltyAccountSummary>.Ok(new LoyaltyAccountSummary
                {
                    BusinessId = Guid.Empty,
                    BusinessName = string.Empty,
                    PointsBalance = payload.NewBalance ?? 0,
                    LastAccrualAtUtc = DateTime.UtcNow
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<LoyaltyAccountSummary>.Fail($"Network error while confirming redemption: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<LoyaltyAccountSummary>>> GetMyAccountsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _apiClient
                    .GetResultAsync<IReadOnlyList<LoyaltyAccountSummary>>(ApiRoutes.Loyalty.GetMyAccounts, cancellationToken)
                    .ConfigureAwait(false);

                if (!response.Succeeded || response.Value is null)
                    return Result<IReadOnlyList<LoyaltyAccountSummary>>.Fail(response.Error ?? "Request failed.");

                return Result<IReadOnlyList<LoyaltyAccountSummary>>.Ok(response.Value);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<LoyaltyAccountSummary>>.Fail($"Network error while retrieving accounts: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<IReadOnlyList<PointsTransaction>>> GetMyHistoryAsync(Guid businessId, CancellationToken cancellationToken)
        {
            if (businessId == Guid.Empty)
                return Result<IReadOnlyList<PointsTransaction>>.Fail("Invalid business identifier.");

            try
            {
                var response = await _apiClient
                    .GetResultAsync<IReadOnlyList<PointsTransaction>>(ApiRoutes.Loyalty.GetMyHistory(businessId), cancellationToken)
                    .ConfigureAwait(false);

                if (!response.Succeeded || response.Value is null)
                    return Result<IReadOnlyList<PointsTransaction>>.Fail(response.Error ?? "Request failed.");

                return Result<IReadOnlyList<PointsTransaction>>.Ok(response.Value);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<PointsTransaction>>.Fail($"Network error while retrieving history: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<Darwin.Contracts.Loyalty.MyLoyaltyBusinessesResponse>> GetMyBusinessesAsync(int page, int pageSize, bool includeInactive, CancellationToken cancellationToken)
        {
            if (page <= 0)
                return Result<Darwin.Contracts.Loyalty.MyLoyaltyBusinessesResponse>.Fail("Page must be a positive integer.");
            if (pageSize <= 0 || pageSize > 200)
                return Result<Darwin.Contracts.Loyalty.MyLoyaltyBusinessesResponse>.Fail("PageSize must be between 1 and 200.");

            var route = $"{ApiRoutes.Loyalty.GetMyBusinesses}?page={page}&pageSize={pageSize}&includeInactiveBusinesses={(includeInactive ? "true" : "false")}";

            try
            {
                var response = await _apiClient
                    .GetResultAsync<Darwin.Contracts.Loyalty.MyLoyaltyBusinessesResponse>(route, cancellationToken)
                    .ConfigureAwait(false);

                if (!response.Succeeded || response.Value is null)
                    return Result<Darwin.Contracts.Loyalty.MyLoyaltyBusinessesResponse>.Fail(response.Error ?? "Request failed.");

                return Result<Darwin.Contracts.Loyalty.MyLoyaltyBusinessesResponse>.Ok(response.Value);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<Darwin.Contracts.Loyalty.MyLoyaltyBusinessesResponse>.Fail($"Network error while retrieving my businesses: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<GetMyLoyaltyTimelinePageResponse>> GetMyLoyaltyTimelinePageAsync(GetMyLoyaltyTimelinePageRequest request, CancellationToken cancellationToken)
        {
            if (request is null)
                return Result<GetMyLoyaltyTimelinePageResponse>.Fail("Request body is required.");

            try
            {
                var response = await _apiClient
                    .PostResultAsync<GetMyLoyaltyTimelinePageRequest, GetMyLoyaltyTimelinePageResponse>(
                        ApiRoutes.Loyalty.GetMyTimeline,
                        request,
                        cancellationToken).ConfigureAwait(false);

                if (!response.Succeeded || response.Value is null)
                    return Result<GetMyLoyaltyTimelinePageResponse>.Fail(response.Error ?? "Request failed.");

                return Result<GetMyLoyaltyTimelinePageResponse>.Ok(response.Value);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<GetMyLoyaltyTimelinePageResponse>.Fail($"Network error while retrieving timeline: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<LoyaltyAccountSummary>> JoinLoyaltyAsync(Guid businessId, Guid? businessLocationId, CancellationToken cancellationToken)
        {
            if (businessId == Guid.Empty)
                return Result<LoyaltyAccountSummary>.Fail("Invalid business identifier.");

            var body = businessLocationId.HasValue
                ? new JoinLoyaltyRequest { BusinessLocationId = businessLocationId }
                : new JoinLoyaltyRequest();

            try
            {
                var response = await _apiClient
                    .PostResultAsync<JoinLoyaltyRequest, LoyaltyAccountSummary>(
                        ApiRoutes.Loyalty.Join(businessId),
                        body,
                        cancellationToken).ConfigureAwait(false);

                if (!response.Succeeded || response.Value is null)
                    return Result<LoyaltyAccountSummary>.Fail(response.Error ?? "Request failed.");

                return Result<LoyaltyAccountSummary>.Ok(response.Value);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<LoyaltyAccountSummary>.Fail($"Network error while joining loyalty: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<LoyaltyRewardSummary?>> GetNextRewardAsync(Guid businessId, CancellationToken cancellationToken)
        {
            if (businessId == Guid.Empty)
                return Result<LoyaltyRewardSummary?>.Fail("Invalid business identifier.");

            try
            {
                // This endpoint returns 200 with payload or 204 No Content when none.
                var result = await _apiClient.GetResultAsync<LoyaltyRewardSummary>(ApiRoutes.Loyalty.GetNextReward(businessId), cancellationToken)
                    .ConfigureAwait(false);

                if (result.Succeeded)
                    return Result<LoyaltyRewardSummary?>.Ok(result.Value);

                // When server returns 204, ApiClient returns ApiClient.NoContentResultMessage
                if (string.Equals(result.Error, ApiClient.NoContentResultMessage, StringComparison.OrdinalIgnoreCase))
                    return Result<LoyaltyRewardSummary?>.Ok(null);

                return Result<LoyaltyRewardSummary?>.Fail(result.Error ?? "Request failed.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<LoyaltyRewardSummary?>.Fail($"Network error while retrieving next reward: {ex.Message}");
            }
        }
    }
}