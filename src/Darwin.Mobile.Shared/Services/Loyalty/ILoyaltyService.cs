using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Models.Loyalty;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Services.Loyalty
{
    /// <summary>
    /// High-level loyalty facade used by both Consumer and Business mobile apps.
    /// Internally this service maps to Darwin.WebApi endpoints and Contracts,
    /// but view models only depend on this abstraction.
    /// </summary>
    public interface ILoyaltyService
    {
        /// <summary>
        /// Prepares a scan session for the specified business and mode
        /// (accrual or redemption) from the consumer app perspective.
        /// This will typically allocate a short-lived token and return
        /// basic context to render the QR code.
        /// </summary>
        /// <param name="businessId">The target business identifier.</param>
        /// <param name="mode">The desired scan mode.</param>
        /// <param name="selectedRewardIds">
        /// Optional reward identifiers to redeem. Only meaningful for redemption mode.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// A result wrapping the prepared scan session client model; on failure,
        /// <see cref="Result.Error"/> will explain the reason in a user-friendly way.
        /// </returns>
        Task<Result<ScanSessionClientModel>> PrepareScanSessionAsync(
            Guid businessId,
            LoyaltyScanMode mode,
            IReadOnlyCollection<Guid>? selectedRewardIds,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a summary of the consumer's loyalty account for the given business.
        /// </summary>
        /// <param name="businessId">The business identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The current account summary on success.</returns>
        Task<Result<LoyaltyAccountSummary>> GetAccountSummaryAsync(
            Guid businessId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the list of rewards that can be selected for the given business,
        /// optionally filtered by the consumer's current balance on the server.
        /// </summary>
        /// <param name="businessId">The business identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of reward summaries on success.</returns>
        Task<Result<IReadOnlyList<LoyaltyRewardSummary>>> GetAvailableRewardsAsync(
            Guid businessId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Processes a scanned QR token from the business app and resolves it
        /// to a scan session on the server, including loyalty state and allowed actions.
        /// </summary>
        /// <param name="qrToken">The opaque QR token that was scanned.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// A client model representing the session from the business perspective.
        /// </returns>
        Task<Result<BusinessScanSessionClientModel>> ProcessScanSessionForBusinessAsync(
            string qrToken,
            CancellationToken cancellationToken);

        /// <summary>
        /// Confirms accrual of points for the current scan session.
        /// </summary>
        /// <param name="sessionToken">
        /// The opaque session token that identifies the scan session.
        /// </param>
        /// <param name="points">
        /// The number of points to accrue. This may represent either a fixed
        /// per-visit rule or a value derived from the transaction amount.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// The updated account summary, including the new balance, on success.
        /// </returns>
        Task<Result<LoyaltyAccountSummary>> ConfirmAccrualAsync(
            string sessionToken,
            int points,
            CancellationToken cancellationToken);

        /// <summary>
        /// Confirms redemption of rewards for the current scan session.
        /// The server determines which rewards were attached to the session.
        /// </summary>
        /// <param name="sessionToken">
        /// The opaque session token that identifies the scan session.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// The updated account summary, including the new balance, on success.
        /// </returns>
        Task<Result<LoyaltyAccountSummary>> ConfirmRedemptionAsync(
            string sessionToken,
            CancellationToken cancellationToken);

        // My accounts (consumer)
        Task<Result<IReadOnlyList<LoyaltyAccountSummary>>> GetMyAccountsAsync(CancellationToken cancellationToken);

        // My history (consumer)
        Task<Result<IReadOnlyList<PointsTransaction>>> GetMyHistoryAsync(Guid businessId, CancellationToken cancellationToken);

        // My businesses (consumer, paged GET with query)
        Task<Result<Darwin.Contracts.Loyalty.MyLoyaltyBusinessesResponse>> GetMyBusinessesAsync(int page, int pageSize, bool includeInactive, CancellationToken cancellationToken);

        // My timeline (consumer, cursor POST)
        Task<Result<GetMyLoyaltyTimelinePageResponse>> GetMyLoyaltyTimelinePageAsync(GetMyLoyaltyTimelinePageRequest request, CancellationToken cancellationToken);

        // Join loyalty (consumer)
        Task<Result<LoyaltyAccountSummary>> JoinLoyaltyAsync(Guid businessId, Guid? businessLocationId, CancellationToken cancellationToken);

        // Next reward (consumer) - 204 when none
        Task<Result<LoyaltyRewardSummary?>> GetNextRewardAsync(Guid businessId, CancellationToken cancellationToken);
    }
}
