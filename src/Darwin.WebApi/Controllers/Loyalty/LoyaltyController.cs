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
    /// API endpoints for the loyalty system used by both consumer and business
    /// mobile applications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The consumer app primarily uses the <c>/scan/prepare</c> endpoint to
    /// create a short-lived scan session that is encoded into a QR code.
    /// </para>
    /// <para>
    /// The business (staff) app uses the remaining endpoints to process the
    /// scanned session, confirm accrual or redemption, and browse loyalty
    /// accounts and history.
    /// </para>
    /// </remarks>
    [ApiController]
    [Route("api/loyalty")]
    [Authorize]
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
        /// Initializes a new instance of the <see cref="LoyaltyController"/> class.
        /// </summary>
        /// <param name="prepareScanSessionHandler">Handler that prepares consumer scan sessions.</param>
        /// <param name="processScanSessionForBusinessHandler">Handler that materializes a scan session for business processing.</param>
        /// <param name="confirmAccrualFromSessionHandler">Handler that confirms accrual for a previously prepared scan session.</param>
        /// <param name="confirmRedemptionFromSessionHandler">Handler that confirms redemption for a previously prepared scan session.</param>
        /// <param name="getMyLoyaltyAccountsHandler">Query handler that returns loyalty accounts for the current user.</param>
        /// <param name="getMyLoyaltyHistoryHandler">Query handler that returns loyalty history for the current user.</param>
        /// <param name="getLoyaltyAccountByBusinessAndUserHandler">Query handler that returns the account for a given business/user pair.</param>
        /// <param name="getAvailableLoyaltyRewardsForBusinessHandler">Query handler that lists available rewards for a business.</param>
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
        /// Prepares a scan session for the current consumer user.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The consumer app calls this endpoint to obtain a short-lived scan
        /// session identifier that is embedded into a QR code. The business app
        /// will later scan this QR code and process it via
        /// <see cref="ProcessScanSessionForBusinessAsync"/>.
        /// </para>
        /// <para>
        /// For redemption mode, the selected rewards are validated and encoded
        /// into the scan session snapshot so that the business device can
        /// safely confirm the operation.
        /// </para>
        /// </remarks>
        /// <param name="request">The scan preparation request coming from the consumer device.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>QR payload and basic session data.</returns>
        [HttpPost("scan/prepare")]
        [ProducesResponseType(typeof(PrepareScanSessionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PrepareScanSessionAsync(
            [FromBody] PrepareScanSessionRequest? request,
            CancellationToken ct = default)
        {
            if (request is null)
            {
                return BadRequest("Request body is required.");
            }

            if (request.BusinessId == Guid.Empty)
            {
                return BadRequest("BusinessId is required.");
            }

            if (request.Mode == LoyaltyScanMode.Unknown)
            {
                return BadRequest("Mode must be specified.");
            }

            var dto = new PrepareScanSessionDto
            {
                BusinessId = request.BusinessId,
                Mode = MapScanMode(request.Mode),
                SelectedRewardTierIds = request.SelectedRewardTierIds?.ToList() ?? new List<Guid>()
            };

            var result = await _prepareScanSessionHandler.HandleAsync(dto, ct).ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var value = result.Value;

            var response = new PrepareScanSessionResponse
            {
                ScanSessionId = value.ScanSessionId,
                ExpiresAtUtc = value.ExpiresAtUtc
            };

            return Ok(response);
        }

        /// <summary>
        /// Processes a scan session from the business (staff) mobile app.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The business app calls this endpoint after scanning the QR code
        /// presented by the consumer. The handler validates that the scan
        /// session is still valid, belongs to the current business, and
        /// materializes a human-friendly view with available actions.
        /// </para>
        /// </remarks>
        /// <param name="request">The scan session processing request.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Information about the scan session and allowed actions.</returns>
        [HttpPost("scan/process")]
        [ProducesResponseType(typeof(ProcessScanSessionForBusinessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ProcessScanSessionForBusinessAsync(
            [FromBody] ProcessScanSessionForBusinessRequest? request,
            CancellationToken ct = default)
        {
            if (request is null)
            {
                return BadRequest("Request body is required.");
            }

            if (request.ScanSessionId == Guid.Empty)
            {
                return BadRequest("ScanSessionId is required.");
            }

            if (request.BusinessId == Guid.Empty)
            {
                return BadRequest("BusinessId is required.");
            }

            var result = await _processScanSessionForBusinessHandler
                .HandleAsync(request.ScanSessionId, request.BusinessId, ct)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var value = result.Value;

            var allowedActions = new LoyaltyScanAllowedActions
            {
                CanAccrue = value.AllowedActions.CanAccrue,
                CanRedeem = value.AllowedActions.CanRedeem
            };

            var response = new ProcessScanSessionForBusinessResponse
            {
                ScanSessionId = value.ScanSessionId,
                Mode = MapScanMode(value.Mode),
                ConsumerUserId = value.ConsumerUserId,
                AccountId = value.AccountId,
                PointsBalance = value.PointsBalance,
                PointsToAccrue = value.PointsToAccrue,
                PointsToRedeem = value.PointsToRedeem,
                SelectedRewards = value.SelectedRewards
                    .Select(r => new LoyaltyRewardSummary
                    {
                        LoyaltyRewardTierId = r.LoyaltyRewardTierId,
                        BusinessId = r.BusinessId,
                        Name = r.Name,
                        Description = r.Description,
                        RequiredPoints = r.RequiredPoints,
                        IsActive = r.IsActive,
                        RequiresConfirmation = r.RequiresConfirmation,
                        IsSelectable = r.IsSelectable
                    })
                    .ToList(),
                AllowedActions = allowedActions
            };

            return Ok(response);
        }

        /// <summary>
        /// Confirms point accrual based on a previously prepared scan session.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This endpoint is called by the business app after the cashier
        /// confirms that the purchase should earn loyalty points. It finalizes
        /// the accrual operation and updates the account balance.
        /// </para>
        /// </remarks>
        /// <param name="request">Accrual confirmation request.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Updated balance information.</returns>
        [HttpPost("scan/confirm-accrual")]
        [ProducesResponseType(typeof(ConfirmAccrualResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmAccrualAsync(
            [FromBody] ConfirmAccrualRequest? request,
            CancellationToken ct = default)
        {
            if (request is null)
            {
                return BadRequest("Request body is required.");
            }

            if (request.ScanSessionId == Guid.Empty)
            {
                return BadRequest("ScanSessionId is required.");
            }

            if (request.BusinessId == Guid.Empty)
            {
                return BadRequest("BusinessId is required.");
            }

            var dto = new ConfirmAccrualFromSessionDto
            {
                ScanSessionId = request.ScanSessionId,
                BusinessId = request.BusinessId,
                BusinessLocationId = request.BusinessLocationId,
                OrderId = request.OrderId,
                Notes = request.Notes
            };

            var result = await _confirmAccrualFromSessionHandler
                .HandleAsync(dto, ct)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var value = result.Value;

            var response = new ConfirmAccrualResponse
            {
                LoyaltyAccountId = value.LoyaltyAccountId,
                NewPointsBalance = value.NewPointsBalance,
                LifetimePoints = value.LifetimePoints
            };

            return Ok(response);
        }

        /// <summary>
        /// Confirms point redemption based on a previously prepared scan session.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The business app calls this endpoint once the cashier has verified
        /// that the selected rewards can be redeemed. The handler performs
        /// final validation and adjusts the account balance.
        /// </para>
        /// </remarks>
        /// <param name="request">Redemption confirmation request.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Updated balance and redemption details.</returns>
        [HttpPost("scan/confirm-redemption")]
        [ProducesResponseType(typeof(ConfirmRedemptionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmRedemptionAsync(
            [FromBody] ConfirmRedemptionRequest? request,
            CancellationToken ct = default)
        {
            if (request is null)
            {
                return BadRequest("Request body is required.");
            }

            if (request.ScanSessionId == Guid.Empty)
            {
                return BadRequest("ScanSessionId is required.");
            }

            if (request.BusinessId == Guid.Empty)
            {
                return BadRequest("BusinessId is required.");
            }

            var dto = new ConfirmRedemptionFromSessionDto
            {
                ScanSessionId = request.ScanSessionId,
                BusinessId = request.BusinessId,
                BusinessLocationId = request.BusinessLocationId,
                OrderId = request.OrderId,
                Notes = request.Notes
            };

            var result = await _confirmRedemptionFromSessionHandler
                .HandleAsync(dto, ct)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var value = result.Value;

            var response = new ConfirmRedemptionResponse
            {
                LoyaltyAccountId = value.LoyaltyAccountId,
                NewPointsBalance = value.NewPointsBalance,
                LifetimePoints = value.LifetimePoints,
                RedeemedPoints = value.RedeemedPoints
            };

            return Ok(response);
        }

        /// <summary>
        /// Gets the current loyalty account of the authenticated consumer for a business.
        /// </summary>
        /// <param name="businessId">Business identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The account if present; 404 otherwise.</returns>
        [HttpGet("account/{businessId:guid}")]
        [ProducesResponseType(typeof(LoyaltyAccountSummary), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentAccountForBusinessAsync(
            Guid businessId,
            CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return BadRequest("BusinessId is required.");
            }

            var result = await _getMyLoyaltyAccountsHandler
                .HandleAsync(ct)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var account = result.Value
                .FirstOrDefault(a => a.BusinessId == businessId);

            if (account is null)
            {
                return NotFound();
            }

            var response = new LoyaltyAccountSummary
            {
                LoyaltyAccountId = account.LoyaltyAccountId,
                BusinessId = account.BusinessId,
                Status = account.Status,
                PointsBalance = account.PointsBalance,
                LifetimePoints = account.LifetimePoints,
                LastAccrualAtUtc = account.LastAccrualAtUtc
            };

            return Ok(response);
        }

        /// <summary>
        /// Lists rewards available for the specified business, taking the
        /// current account balance into account.
        /// </summary>
        /// <param name="businessId">Business identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>List of rewards ready to be displayed in the business app.</returns>
        [HttpGet("business/{businessId:guid}/rewards")]
        [ProducesResponseType(typeof(IReadOnlyList<LoyaltyRewardSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetRewardsForBusinessAsync(
            Guid businessId,
            CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return BadRequest("BusinessId is required.");
            }

            var result = await _getAvailableLoyaltyRewardsForBusinessHandler
                .HandleAsync(businessId, ct)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var response = result.Value
                .Select(r => new LoyaltyRewardSummary
                {
                    LoyaltyRewardTierId = r.LoyaltyRewardTierId,
                    BusinessId = r.BusinessId,
                    Name = r.Name,
                    Description = r.Description,
                    RequiredPoints = r.RequiredPoints,
                    IsActive = r.IsActive,
                    RequiresConfirmation = r.RequiresConfirmation,
                    IsSelectable = r.IsSelectable
                })
                .ToList();

            return Ok(response);
        }

        /// <summary>
        /// Returns all loyalty accounts for the current authenticated consumer.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>List of loyalty accounts.</returns>
        [HttpGet("my/accounts")]
        [ProducesResponseType(typeof(IReadOnlyList<LoyaltyAccountSummary>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyAccountsAsync(
            CancellationToken ct = default)
        {
            var result = await _getMyLoyaltyAccountsHandler
                .HandleAsync(ct)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var items = result.Value
                .Select(a => new LoyaltyAccountSummary
                {
                    LoyaltyAccountId = a.LoyaltyAccountId,
                    BusinessId = a.BusinessId,
                    Status = a.Status,
                    PointsBalance = a.PointsBalance,
                    LifetimePoints = a.LifetimePoints,
                    LastAccrualAtUtc = a.LastAccrualAtUtc
                })
                .ToList();

            return Ok(items);
        }

        /// <summary>
        /// Returns the loyalty transaction history for the current user.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Chronological list of loyalty transactions.</returns>
        [HttpGet("my/history")]
        [ProducesResponseType(typeof(IReadOnlyList<LoyaltyTransactionItem>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyHistoryAsync(
            CancellationToken ct = default)
        {
            var result = await _getMyLoyaltyHistoryHandler
                .HandleAsync(ct)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var items = result.Value
                .Select(h => new LoyaltyTransactionItem
                {
                    LoyaltyAccountId = h.LoyaltyAccountId,
                    BusinessId = h.BusinessId,
                    OccurredAtUtc = h.OccurredAtUtc,
                    DeltaPoints = h.DeltaPoints,
                    BalanceAfter = h.BalanceAfter,
                    Kind = h.Kind,
                    Description = h.Description
                })
                .ToList();

            return Ok(items);
        }

        /// <summary>
        /// Maps a domain-level scan mode to its contract representation.
        /// </summary>
        /// <param name="mode">Domain scan mode.</param>
        /// <returns>Contract scan mode.</returns>
        private static LoyaltyScanMode MapScanMode(DomainScanMode mode)
        {
            return mode switch
            {
                DomainScanMode.Unknown => LoyaltyScanMode.Unknown,
                DomainScanMode.Accrual => LoyaltyScanMode.Accrual,
                DomainScanMode.Redemption => LoyaltyScanMode.Redemption,
                _ => LoyaltyScanMode.Unknown
            };
        }

        /// <summary>
        /// Maps a contract-level scan mode to its domain representation.
        /// </summary>
        /// <param name="mode">Contract scan mode.</param>
        /// <returns>Domain scan mode.</returns>
        private static DomainScanMode MapScanMode(LoyaltyScanMode mode)
        {
            return mode switch
            {
                LoyaltyScanMode.Unknown => DomainScanMode.Unknown,
                LoyaltyScanMode.Accrual => DomainScanMode.Accrual,
                LoyaltyScanMode.Redemption => DomainScanMode.Redemption,
                _ => DomainScanMode.Unknown
            };
        }

        /// <summary>
        /// Translates a non-generic <see cref="Result"/> into a problem response.
        /// </summary>
        /// <param name="result">The operation result.</param>
        /// <returns>HTTP 400 problem response.</returns>
        private static IActionResult ProblemFromResult(Result result)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.Succeeded)
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            var error = result.Error ?? "Unknown error.";

            return new ObjectResult(new { error })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        /// <summary>
        /// Translates a generic <see cref="Result{T}"/> into a problem response.
        /// </summary>
        /// <typeparam name="T">Result payload type.</typeparam>
        /// <param name="result">The operation result.</param>
        /// <returns>HTTP 400 problem response.</returns>
        private static IActionResult ProblemFromResult<T>(Result<T> result)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.Succeeded)
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            var error = result.Error ?? "Unknown error.";

            return new ObjectResult(new { error })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        // Alias to keep domain/contract scan modes distinct in method signatures.
        private enum DomainScanMode
        {
            Unknown = 0,
            Accrual = 1,
            Redemption = 2
        }
    }
}
