using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Loyalty.Commands;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Queries;
using Darwin.Contracts.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebApi.Controllers.Loyalty
{
    /// <summary>
    ///     Exposes Loyalty-related endpoints for mobile applications (consumer and business).
    ///     This controller acts as a thin mapping layer between public wire contracts
    ///     defined in Darwin.Contracts and the internal Application-layer handlers.
    /// </summary>
    [ApiController]
    [Route("api/loyalty")]
    [Authorize] // All endpoints require an authenticated user; fine-grained permission checks are done in handlers.
    public sealed class LoyaltyController : ControllerBase
    {
        private readonly PrepareScanSessionHandler _prepareScanSessionHandler;
        private readonly ProcessScanSessionForBusinessHandler _processScanSessionForBusinessHandler;
        private readonly ConfirmAccrualFromSessionHandler _confirmAccrualFromSessionHandler;
        private readonly ConfirmRedemptionFromSessionHandler _confirmRedemptionFromSessionHandler;
        private readonly GetMyLoyaltyAccountsHandler _getMyLoyaltyAccountsHandler;
        private readonly GetMyLoyaltyHistoryHandler _getMyLoyaltyHistoryHandler;
        private readonly GetLoyaltyAccountByBusinessAndUserHandler _getLoyaltyAccountByBusinessAndUserHandler;
        private readonly GetAvailableLoyaltyRewardsForBusinessHandler _getAvailableLoyaltyRewardsForBusinessHandler;

        /// <summary>
        ///     Initializes a new instance of the <see cref="LoyaltyController"/> class.
        ///     All dependencies are concrete Application-layer handlers that encapsulate
        ///     the domain logic and persistence concerns.
        /// </summary>
        /// <param name="prepareScanSessionHandler">Handler responsible for preparing a scan session.</param>
        /// <param name="processScanSessionForBusinessHandler">Handler that processes a scan session from the business side.</param>
        /// <param name="confirmAccrualFromSessionHandler">Handler that confirms point accrual for an already prepared scan session.</param>
        /// <param name="confirmRedemptionFromSessionHandler">Handler that confirms reward redemption for an already prepared scan session.</param>
        /// <param name="getMyLoyaltyAccountsHandler">Handler that retrieves all loyalty accounts for the current user.</param>
        /// <param name="getMyLoyaltyHistoryHandler">Handler that retrieves the loyalty transaction history for the current user and a specific business.</param>
        /// <param name="getLoyaltyAccountByBusinessAndUserHandler">Handler that returns a single loyalty account for a given business and user.</param>
        /// <param name="getAvailableLoyaltyRewardsForBusinessHandler">Handler that returns the configured reward tiers for a specific business.</param>
        public LoyaltyController(
            PrepareScanSessionHandler prepareScanSessionHandler,
            ProcessScanSessionForBusinessHandler processScanSessionForBusinessHandler,
            ConfirmAccrualFromSessionHandler confirmAccrualFromSessionHandler,
            ConfirmRedemptionFromSessionHandler confirmRedemptionFromSessionHandler,
            GetMyLoyaltyAccountsHandler getMyLoyaltyAccountsHandler,
            GetMyLoyaltyHistoryHandler getMyLoyaltyHistoryHandler,
            GetLoyaltyAccountByBusinessAndUserHandler getLoyaltyAccountByBusinessAndUserHandler,
            GetAvailableLoyaltyRewardsForBusinessHandler getAvailableLoyaltyRewardsForBusinessHandler)
        {
            _prepareScanSessionHandler = prepareScanSessionHandler ?? throw new ArgumentNullException(nameof(prepareScanSessionHandler));
            _processScanSessionForBusinessHandler = processScanSessionForBusinessHandler ?? throw new ArgumentNullException(nameof(processScanSessionForBusinessHandler));
            _confirmAccrualFromSessionHandler = confirmAccrualFromSessionHandler ?? throw new ArgumentNullException(nameof(confirmAccrualFromSessionHandler));
            _confirmRedemptionFromSessionHandler = confirmRedemptionFromSessionHandler ?? throw new ArgumentNullException(nameof(confirmRedemptionFromSessionHandler));
            _getMyLoyaltyAccountsHandler = getMyLoyaltyAccountsHandler ?? throw new ArgumentNullException(nameof(getMyLoyaltyAccountsHandler));
            _getMyLoyaltyHistoryHandler = getMyLoyaltyHistoryHandler ?? throw new ArgumentNullException(nameof(getMyLoyaltyHistoryHandler));
            _getLoyaltyAccountByBusinessAndUserHandler = getLoyaltyAccountByBusinessAndUserHandler ?? throw new ArgumentNullException(nameof(getLoyaltyAccountByBusinessAndUserHandler));
            _getAvailableLoyaltyRewardsForBusinessHandler = getAvailableLoyaltyRewardsForBusinessHandler ?? throw new ArgumentNullException(nameof(getAvailableLoyaltyRewardsForBusinessHandler));
        }

        /// <summary>
        ///     Prepares a loyalty scan session for the current user on the consumer mobile app.
        ///     This endpoint is called by the consumer app before showing the QR/Barcode to the business device.
        /// </summary>
        /// <param name="request">The request describing the business, optional location, scan mode and pre-selected rewards.</param>
        /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
        /// <returns>
        ///     On success, returns a <see cref="PrepareScanSessionResponse"/> with the scan session identifier,
        ///     expiration timestamp and current points balance. On error, returns a ProblemDetails with HTTP 400.
        /// </returns>
        [HttpPost("scan/prepare")]
        [ProducesResponseType(typeof(PrepareScanSessionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PrepareScanSessionAsync(
            [FromBody] PrepareScanSessionRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest("Request body must not be null.");
            }

            var dto = new PrepareScanSessionDto
            {
                BusinessId = request.BusinessId,
                BusinessLocationId = request.BusinessLocationId,
                Mode = (LoyaltyScanMode)request.Mode,
                SelectedRewardTierIds = request.SelectedRewardTierIds?.ToList() ?? new List<Guid>(),
                DeviceId = request.DeviceId
            };

            Result<ScanSessionPreparedDto> result =
                await _prepareScanSessionHandler.HandleAsync(dto, cancellationToken).ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var prepared = result.Value;

            var response = new PrepareScanSessionResponse
            {
                ScanSessionId = prepared.ScanSessionId,
                Mode = (Contracts.Loyalty.LoyaltyScanMode)prepared.Mode,
                ExpiresAtUtc = prepared.ExpiresAtUtc,
                CurrentPointsBalance = prepared.CurrentPointsBalance,
                // At the moment the pre-selected rewards are only tracked on the client side.
                // The Application handler does not yet return them, so we deliberately keep this empty.
                SelectedRewards = Array.Empty<LoyaltyRewardSummary>()
            };

            return Ok(response);
        }

        /// <summary>
        ///     Processes a scan session from the business (staff) mobile app.
        ///     This validates the scan session, resolves the loyalty account and determines the allowed actions
        ///     (accrual, self-redemption, pending redemption, etc.) for the given business.
        /// </summary>
        /// <param name="request">The business-side request with scan session id, business and optional location.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        ///     On success, returns a <see cref="ProcessScanSessionForBusinessResponse"/> that describes the business view
        ///     of the scan session (account summary, points, pending reward, allowed actions).
        /// </returns>
        [HttpPost("scan/process-business")]
        [ProducesResponseType(typeof(ProcessScanSessionForBusinessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ProcessScanSessionForBusinessAsync(
            [FromBody] ProcessScanSessionForBusinessRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest("Request body must not be null.");
            }

            var dto = new ProcessScanSessionForBusinessDto
            {
                ScanSessionId = request.ScanSessionId,
                BusinessId = request.BusinessId,
                BusinessLocationId = request.BusinessLocationId
            };

            Result<ScanSessionBusinessViewDto> result =
                await _processScanSessionForBusinessHandler.HandleAsync(dto, cancellationToken).ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var view = result.Value;

            var response = new ProcessScanSessionForBusinessResponse
            {
                ScanSessionId = view.ScanSessionId,
                BusinessId = view.BusinessId,
                BusinessName = view.BusinessName,
                LoyaltyAccountId = view.LoyaltyAccountId,
                CurrentPointsBalance = view.CurrentPointsBalance,
                LifetimePoints = view.LifetimePoints,
                PendingRewardTitle = view.PendingRewardTitle,
                PendingRewardPoints = view.PendingRewardPoints,
                AllowedActions = new LoyaltyScanAllowedActions
                {
                    CanAccruePoints = view.AllowedActions.CanAccruePoints,
                    CanRedeemSelfServiceReward = view.AllowedActions.CanRedeemSelfServiceReward,
                    CanConfirmPendingRedemption = view.AllowedActions.CanConfirmPendingRedemption
                }
            };

            return Ok(response);
        }

        /// <summary>
        ///     Confirms point accrual for an already prepared scan session from the business mobile app.
        ///     This is typically invoked when the staff decides how many points to grant after seeing the scan.
        /// </summary>
        /// <param name="request">Accrual confirmation request including scan session, points delta and optional metadata.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        ///     On success, returns <see cref="ConfirmAccrualResponse"/> with updated account balances and transaction information.
        /// </returns>
        [HttpPost("scan/confirm-accrual")]
        [ProducesResponseType(typeof(ConfirmAccrualResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmAccrualAsync(
            [FromBody] ConfirmAccrualRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest("Request body must not be null.");
            }

            var dto = new ConfirmAccrualFromSessionDto
            {
                ScanSessionId = request.ScanSessionId,
                BusinessId = request.BusinessId,
                BusinessLocationId = request.BusinessLocationId,
                PerformedByUserId = request.PerformedByUserId,
                PointsDelta = request.PointsDelta,
                Notes = request.Notes
            };

            Result<ConfirmAccrualFromSessionResultDto> result =
                await _confirmAccrualFromSessionHandler.HandleAsync(dto, cancellationToken).ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var data = result.Value;

            var response = new ConfirmAccrualResponse
            {
                ScanSessionId = data.ScanSessionId,
                LoyaltyAccountId = data.LoyaltyAccountId,
                NewPointsBalance = data.NewPointsBalance,
                NewLifetimePoints = data.NewLifetimePoints,
                TransactionId = data.TransactionId
            };

            return Ok(response);
        }

        /// <summary>
        ///     Confirms reward redemption for an already prepared scan session from the business mobile app.
        ///     This is typically invoked when a pending or self-service redemption is finally granted to the customer.
        /// </summary>
        /// <param name="request">Redemption confirmation request with scan session, business context and optional metadata.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        ///     On success, returns <see cref="ConfirmRedemptionResponse"/> with updated balances and redemption details.
        /// </returns>
        [HttpPost("scan/confirm-redemption")]
        [ProducesResponseType(typeof(ConfirmRedemptionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmRedemptionAsync(
            [FromBody] ConfirmRedemptionRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest("Request body must not be null.");
            }

            var dto = new ConfirmRedemptionFromSessionDto
            {
                ScanSessionId = request.ScanSessionId,
                BusinessId = request.BusinessId,
                BusinessLocationId = request.BusinessLocationId,
                PerformedByUserId = request.PerformedByUserId
            };

            Result<ConfirmRedemptionFromSessionResultDto> result =
                await _confirmRedemptionFromSessionHandler.HandleAsync(dto, cancellationToken).ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var data = result.Value;

            var response = new ConfirmRedemptionResponse
            {
                ScanSessionId = data.ScanSessionId,
                LoyaltyAccountId = data.LoyaltyAccountId,
                NewPointsBalance = data.NewPointsBalance,
                NewLifetimePoints = data.NewLifetimePoints,
                RedemptionId = data.RedemptionId,
                TransactionId = data.TransactionId
            };

            return Ok(response);
        }

        /// <summary>
        ///     Returns the loyalty account summary for the current user and a specific business.
        ///     This is used by the consumer app on the business detail screen.
        /// </summary>
        /// <param name="businessId">The business identifier for which the account information is requested.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        ///     On success, returns a <see cref="LoyaltyAccountSummary"/> for this business.
        /// </returns>
        [HttpGet("account/{businessId:guid}")]
        [ProducesResponseType(typeof(LoyaltyAccountSummary), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAccountForBusinessAsync(
            [FromRoute] Guid businessId,
            CancellationToken cancellationToken)
        {
            Result<LoyaltyAccountSummaryDto?> result =
                await _getLoyaltyAccountByBusinessAndUserHandler.HandleAsync(businessId, cancellationToken)
                    .ConfigureAwait(false);

            if (!result.Succeeded)
            {
                return ProblemFromResult(result);
            }

            if (result.Value is null)
            {
                // For a business with no existing account, we return 200 with a null body to allow the client
                // to differentiate between "no account yet" and a hard error.
                return Ok(null);
            }

            var dto = result.Value;

            var response = new LoyaltyAccountSummary
            {
                LoyaltyAccountId = dto.LoyaltyAccountId,
                BusinessId = dto.BusinessId,
                BusinessName = dto.BusinessName,
                CurrentPointsBalance = dto.CurrentPointsBalance,
                LifetimePoints = dto.LifetimePoints
            };

            return Ok(response);
        }

        /// <summary>
        ///     Returns the configured loyalty rewards (tiers) for a specific business.
        ///     This is used by the consumer app when showing the list of available rewards.
        /// </summary>
        /// <param name="businessId">The business identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        ///     On success, returns a sequence of <see cref="LoyaltyRewardSummary"/> items.
        /// </returns>
        [HttpGet("rewards/{businessId:guid}")]
        [ProducesResponseType(typeof(IReadOnlyList<LoyaltyRewardSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetRewardsForBusinessAsync(
            [FromRoute] Guid businessId,
            CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<LoyaltyRewardSummaryDto>> result =
                await _getAvailableLoyaltyRewardsForBusinessHandler.HandleAsync(businessId, cancellationToken)
                    .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var rewards = result.Value
                .Select(r => new LoyaltyRewardSummary
                {
                    LoyaltyRewardTierId = r.LoyaltyRewardTierId,
                    Title = r.Title,
                    Description = r.Description,
                    PointsRequired = r.PointsRequired,
                    AllowSelfRedemption = r.AllowSelfRedemption
                })
                .ToArray();

            return Ok(rewards);
        }

        /// <summary>
        ///     Returns all loyalty accounts for the current user across all businesses.
        ///     This is useful for "My Loyalty" overview screens in the consumer app.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        ///     On success, returns a list of <see cref="LoyaltyAccountSummary"/> objects.
        /// </returns>
        [HttpGet("my-accounts")]
        [ProducesResponseType(typeof(IReadOnlyList<LoyaltyAccountSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMyAccountsAsync(CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<LoyaltyAccountSummaryDto>> result =
                await _getMyLoyaltyAccountsHandler.HandleAsync(cancellationToken).ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var accounts = result.Value
                .Select(a => new LoyaltyAccountSummary
                {
                    LoyaltyAccountId = a.LoyaltyAccountId,
                    BusinessId = a.BusinessId,
                    BusinessName = a.BusinessName,
                    CurrentPointsBalance = a.CurrentPointsBalance,
                    LifetimePoints = a.LifetimePoints
                })
                .ToArray();

            return Ok(accounts);
        }

        /// <summary>
        ///     Returns the loyalty transaction history for the current user and a given business.
        ///     This is used by the consumer app for the "History" view per business.
        /// </summary>
        /// <param name="businessId">The business identifier for which to return the history.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        ///     On success, returns a list of <see cref="LoyaltyTransactionItem"/> records.
        /// </returns>
        [HttpGet("my-history/{businessId:guid}")]
        [ProducesResponseType(typeof(IReadOnlyList<LoyaltyTransactionItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMyHistoryAsync(
            [FromRoute] Guid businessId,
            CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<LoyaltyPointsTransactionDto>> result =
                await _getMyLoyaltyHistoryHandler.HandleAsync(businessId, cancellationToken)
                    .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var items = result.Value
                .Select(t => new LoyaltyTransactionItem
                {
                    TransactionId = t.TransactionId,
                    OccurredAtUtc = t.OccurredAtUtc,
                    PointsDelta = t.PointsDelta,
                    Type = t.Type,
                    Description = t.Description
                })
                .ToArray();

            return Ok(items);
        }

        /// <summary>
        ///     Creates a <see cref="ProblemDetails"/> response from a non-success <see cref="Result"/>.
        ///     This follows the same minimal pattern that is used in the <see cref="Auth.AuthController"/>.
        /// </summary>
        /// <param name="result">The failed result instance.</param>
        /// <returns>An <see cref="IActionResult"/> containing a 400 response with a problem details payload.</returns>
        private IActionResult ProblemFromResult(Result result)
        {
            var details = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Operation failed",
                Detail = string.IsNullOrWhiteSpace(result.Error) ? "An unknown error occurred." : result.Error
            };

            return BadRequest(details);
        }

        /// <summary>
        ///     Overload for <see cref="ProblemFromResult(Result)"/> that accepts a generic <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The result payload type that is ignored for error mapping.</typeparam>
        /// <param name="result">The failed generic result instance.</param>
        /// <returns>An <see cref="IActionResult"/> containing a 400 response with a problem details payload.</returns>
        private IActionResult ProblemFromResult<T>(Result<T> result)
        {
            return ProblemFromResult((Result)result);
        }
    }
}
