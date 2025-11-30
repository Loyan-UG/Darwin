using Darwin.Contracts.Loyalty;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Services
{
    /// <summary>
    /// Facade over WebApi loyalty endpoints used by both mobile apps.
    /// This interface hides HTTP details and exposes task-based, result-wrapped
    /// operations to the view models.
    /// </summary>
    public interface ILoyaltyService
    {
        /// <summary>
        /// Prepares a scan session for the consumer app, returning a QR token
        /// that can be rendered and scanned by the business app.
        /// </summary>
        /// <param name="request">Scan session preparation parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A result containing the prepared scan session payload.</returns>
        Task<Result<PrepareScanSessionResponse>> PrepareScanSessionAsync(
            PrepareScanSessionRequest request,
            CancellationToken cancellationToken);

        /// <summary>
        /// Processes a scan session token in the business app, resolving the
        /// session mode (accrual vs redemption) and any selected rewards.
        /// </summary>
        /// <param name="request">Scan processing parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A result containing scan session details for the business.</returns>
        Task<Result<ProcessScanSessionForBusinessResponse>> ProcessScanSessionForBusinessAsync(
            ProcessScanSessionForBusinessRequest request,
            CancellationToken cancellationToken);

        /// <summary>
        /// Confirms accrual of loyalty points for the given scan session.
        /// </summary>
        /// <param name="request">Accrual confirmation parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A result containing the updated balance or additional details.</returns>
        Task<Result<ConfirmAccrualResponse>> ConfirmAccrualAsync(
            ConfirmAccrualRequest request,
            CancellationToken cancellationToken);

        /// <summary>
        /// Confirms redemption of rewards for the given scan session.
        /// </summary>
        /// <param name="request">Redemption confirmation parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A result indicating success or failure and optional balance.</returns>
        Task<Result<ConfirmRedemptionResponse>> ConfirmRedemptionAsync(
            ConfirmRedemptionRequest request,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the loyalty account summary for the current consumer at the specified business.
        /// </summary>
        /// <param name="businessId">Business identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A result containing the account summary.</returns>
        Task<Result<LoyaltyAccountSummary>> GetAccountSummaryAsync(
            Guid businessId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the list of rewards available for the current consumer at the specified business.
        /// </summary>
        /// <param name="businessId">Business identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A result containing the available rewards.</returns>
        Task<Result<IReadOnlyList<LoyaltyRewardSummary>>> GetAvailableRewardsAsync(
            Guid businessId,
            CancellationToken cancellationToken);
    }
}
