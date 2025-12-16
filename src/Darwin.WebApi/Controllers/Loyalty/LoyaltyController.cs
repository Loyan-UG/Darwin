using Darwin.Application.Loyalty.Commands;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Queries;
using Darwin.Contracts.Loyalty;
using Darwin.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Use explicit aliases to avoid ambiguity between domain and contract enums.
using DomainLoyaltyScanMode = Darwin.Domain.Enums.LoyaltyScanMode;
using ContractLoyaltyScanMode = Darwin.Contracts.Loyalty.LoyaltyScanMode;

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
    [Route("api/v1/loyalty")]

    [Authorize]
    public sealed class LoyaltyController : ControllerBase
    {
        private readonly PrepareScanSessionHandler _prepareScanSessionHandler;
        private readonly ProcessScanSessionForBusinessHandler _processScanSessionForBusinessHandler;
        private readonly ConfirmAccrualFromSessionHandler _confirmAccrualFromSessionHandler;
        private readonly ConfirmRedemptionFromSessionHandler _confirmRedemptionFromSessionHandler;
        private readonly GetMyLoyaltyAccountsHandler _getMyLoyaltyAccountsHandler;
        private readonly GetMyLoyaltyHistoryHandler _getMyLoyaltyHistoryHandler;
        private readonly GetMyLoyaltyAccountForBusinessHandler _getMyLoyaltyAccountForBusinessHandler;
        private readonly GetAvailableLoyaltyRewardsForBusinessHandler _getAvailableLoyaltyRewardsForBusinessHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoyaltyController"/> class.
        /// </summary>
        /// <param name="prepareScanSessionHandler">
        /// Command handler that prepares consumer scan sessions.
        /// </param>
        /// <param name="processScanSessionForBusinessHandler">
        /// Query handler that materializes a scan session for business processing.
        /// </param>
        /// <param name="confirmAccrualFromSessionHandler">
        /// Command handler that confirms accrual for a previously prepared scan session.
        /// </param>
        /// <param name="confirmRedemptionFromSessionHandler">
        /// Command handler that confirms redemption for a previously prepared scan session.
        /// </param>
        /// <param name="getMyLoyaltyAccountsHandler">
        /// Query handler that returns loyalty accounts for the current user.
        /// </param>
        /// <param name="getMyLoyaltyHistoryHandler">
        /// Query handler that returns loyalty history for the current user.
        /// </param>
        /// <param name="getMyLoyaltyAccountForBusinessHandler ">
        /// Query handler that returns the account for a given business/user pair.
        /// </param>
        /// <param name="getAvailableLoyaltyRewardsForBusinessHandler">
        /// Query handler that lists available rewards for a business.
        /// </param>
        public LoyaltyController(
            PrepareScanSessionHandler prepareScanSessionHandler,
            ProcessScanSessionForBusinessHandler processScanSessionForBusinessHandler,
            ConfirmAccrualFromSessionHandler confirmAccrualFromSessionHandler,
            ConfirmRedemptionFromSessionHandler confirmRedemptionFromSessionHandler,
            GetMyLoyaltyAccountsHandler getMyLoyaltyAccountsHandler,
            GetMyLoyaltyHistoryHandler getMyLoyaltyHistoryHandler,
            GetMyLoyaltyAccountForBusinessHandler getMyLoyaltyAccountForBusinessHandler,
            GetAvailableLoyaltyRewardsForBusinessHandler getAvailableLoyaltyRewardsForBusinessHandler)
        {
            _prepareScanSessionHandler = prepareScanSessionHandler 
                ?? throw new ArgumentNullException(nameof(prepareScanSessionHandler));
            _processScanSessionForBusinessHandler = processScanSessionForBusinessHandler 
                ?? throw new ArgumentNullException(nameof(processScanSessionForBusinessHandler));
            _confirmAccrualFromSessionHandler = confirmAccrualFromSessionHandler 
                ?? throw new ArgumentNullException(nameof(confirmAccrualFromSessionHandler));
            _confirmRedemptionFromSessionHandler = confirmRedemptionFromSessionHandler 
                ?? throw new ArgumentNullException(nameof(confirmRedemptionFromSessionHandler));
            _getMyLoyaltyAccountsHandler = getMyLoyaltyAccountsHandler 
                ?? throw new ArgumentNullException(nameof(getMyLoyaltyAccountsHandler));
            _getMyLoyaltyHistoryHandler = getMyLoyaltyHistoryHandler 
                ?? throw new ArgumentNullException(nameof(getMyLoyaltyHistoryHandler));
            _getMyLoyaltyAccountForBusinessHandler = getMyLoyaltyAccountForBusinessHandler 
                ?? throw new ArgumentNullException(nameof(getMyLoyaltyAccountForBusinessHandler));
            _getAvailableLoyaltyRewardsForBusinessHandler = getAvailableLoyaltyRewardsForBusinessHandler 
                ?? throw new ArgumentNullException(nameof(getAvailableLoyaltyRewardsForBusinessHandler));
        }



        #region Scan preparation (consumer)

        /// <summary>
        /// Prepares a new loyalty scan session for the current consumer user.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The consumer app calls this endpoint with a target business and an optional
        /// list of rewards to redeem. The backend resolves the corresponding loyalty
        /// account, creates a short-lived <c>ScanSession</c> and returns the opaque
        /// <c>ScanSessionToken</c> plus basic context.
        /// </para>
        /// <para>
        /// The returned <see cref="PrepareScanSessionResponse.ScanSessionToken"/> value is
        /// the only data that must be encoded into the QR code shown on the device.
        /// No internal identifiers are allowed to cross the API boundary.
        /// </para>
        /// <para>
        /// For redemption-mode sessions, the response also includes a list of
        /// <see cref="LoyaltyRewardSummary"/> instances describing the rewards that
        /// were actually accepted for redemption. This list is derived by joining:
        /// (1) the accepted reward tier ids returned by the Application handler, and
        /// (2) the available rewards query for the business.
        /// </para>
        /// </remarks>
        /// <param name="request">The scan preparation request payload sent by the consumer device.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// <see cref="PrepareScanSessionResponse"/> on success; HTTP 400 with an error payload
        /// when validation or business rules fail.
        /// </returns>
        [HttpPost("scan/prepare")]
        [ProducesResponseType(typeof(PrepareScanSessionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PrepareScanSessionAsync(
            [FromBody] PrepareScanSessionRequest? request,
            CancellationToken ct = default)
        {
            if (request is null)
            {
                return BadRequestError("Request body is required.");
            }

            if (request.BusinessId == Guid.Empty)
            {
                return BadRequestError("BusinessId is required.");
            }

            var dto = new PrepareScanSessionDto
            {
                BusinessId = request.BusinessId,
                BusinessLocationId = request.BusinessLocationId,
                Mode = MapScanMode(request.Mode),
                SelectedRewardTierIds = request.SelectedRewardTierIds?.Where(x => x != Guid.Empty).Distinct().ToList()
                    ?? new List<Guid>(),
                DeviceId = request.DeviceId
            };

            var result = await _prepareScanSessionHandler
                .HandleAsync(dto, ct)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var value = result.Value;

            // Default to an empty list; only redemption-mode sessions with accepted reward tiers populate this.
            IReadOnlyList<LoyaltyRewardSummary> selectedRewards = Array.Empty<LoyaltyRewardSummary>();

            if (value.Mode == DomainLoyaltyScanMode.Redemption &&
                value.SelectedRewardTierIds is { Count: > 0 })
            {
                var availableRewardsResult = await _getAvailableLoyaltyRewardsForBusinessHandler
                    .HandleAsync(request.BusinessId, ct)
                    .ConfigureAwait(false);

                if (availableRewardsResult.Succeeded && availableRewardsResult.Value is not null)
                {
                    var acceptedTierIds = value.SelectedRewardTierIds
                        .Where(x => x != Guid.Empty)
                        .Distinct()
                        .ToHashSet();

                    selectedRewards = availableRewardsResult.Value
                        .Where(r => acceptedTierIds.Contains(r.LoyaltyRewardTierId))
                        .Select(r => new LoyaltyRewardSummary
                        {
                            LoyaltyRewardTierId = r.LoyaltyRewardTierId,
                            BusinessId = r.BusinessId,
                            Name = r.Name ?? string.Empty,
                            Description = r.Description,
                            RequiredPoints = r.RequiredPoints,
                            IsActive = r.IsActive,
                            IsSelectable = r.IsSelectable
                        })
                        .ToList();
                }
            }

            var response = new PrepareScanSessionResponse
            {
                ScanSessionToken = value.ScanSessionToken,
                Mode = MapScanMode(value.Mode),
                ExpiresAtUtc = value.ExpiresAtUtc,
                CurrentPointsBalance = value.CurrentPointsBalance,
                SelectedRewards = selectedRewards
            };

            return Ok(response);
        }

        #endregion



        #region Enum mapping helpers

        /// <summary>
        /// Maps the contract-level scan mode enum to the domain/Application enum.
        /// </summary>
        /// <param name="mode">Contract scan mode value.</param>
        /// <returns>Domain scan mode value.</returns>
        private static DomainLoyaltyScanMode MapScanMode(ContractLoyaltyScanMode mode)
        {
            return mode switch
            {
                ContractLoyaltyScanMode.Accrual => DomainLoyaltyScanMode.Accrual,
                ContractLoyaltyScanMode.Redemption => DomainLoyaltyScanMode.Redemption,
                _ => DomainLoyaltyScanMode.Accrual
            };
        }

        /// <summary>
        /// Maps the domain/Application scan mode enum to the contract-level enum.
        /// </summary>
        /// <param name="mode">Domain scan mode value.</param>
        /// <returns>Contract scan mode value.</returns>
        private static ContractLoyaltyScanMode MapScanMode(DomainLoyaltyScanMode mode)
        {
            return mode switch
            {
                DomainLoyaltyScanMode.Accrual => ContractLoyaltyScanMode.Accrual,
                DomainLoyaltyScanMode.Redemption => ContractLoyaltyScanMode.Redemption,
                _ => ContractLoyaltyScanMode.Accrual
            };
        }

        #endregion


        #region Scan processing (business)

        /// <summary>
        /// Processes a scanned QR token for the current business and returns a business-facing
        /// view of the scan session.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The business app scans a QR code that contains only an opaque <c>ScanSessionToken</c>.
        /// The backend resolves the token to internal records, validates expiry/state/ownership,
        /// and returns the session view.
        /// </para>
        /// </remarks>
        /// <param name="request">Request payload containing <c>ScanSessionToken</c>.</param>
        /// <param name="ct">Cancellation token.</param>
        [HttpPost("scan/process")]
        [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
        [ProducesResponseType(typeof(ProcessScanSessionForBusinessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ProcessScanSessionForBusinessAsync(
            [FromBody] ProcessScanSessionForBusinessRequest? request,
            CancellationToken ct = default)
        {
            if (request is null)
            {
                return BadRequestError("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ScanSessionToken))
            {
                return BadRequestError("ScanSessionToken is required.");
            }

            if (!TryGetCurrentBusinessId(out var businessId, out var errorResult))
            {
                return errorResult ?? Forbid();
            }

            var result = await _processScanSessionForBusinessHandler
                .HandleAsync(request.ScanSessionToken, businessId, ct)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var value = result.Value;

            // Default to empty list; only redemption-mode sessions may carry selected rewards.
            IReadOnlyList<LoyaltyRewardSummary> selectedRewards = Array.Empty<LoyaltyRewardSummary>();

            if (value.Mode == DomainLoyaltyScanMode.Redemption &&
                value.SelectedRewards is { Count: > 0 })
            {
                var availableRewardsResult = await _getAvailableLoyaltyRewardsForBusinessHandler
                    .HandleAsync(businessId, ct)
                    .ConfigureAwait(false);

                if (availableRewardsResult.Succeeded && availableRewardsResult.Value is not null)
                {
                    var selectedTierIds = value.SelectedRewards
                        .Select(x => x.LoyaltyRewardTierId)
                        .Where(x => x != Guid.Empty)
                        .Distinct()
                        .ToHashSet();

                    selectedRewards = availableRewardsResult.Value
                        .Where(r => selectedTierIds.Contains(r.LoyaltyRewardTierId))
                        .Select(r => new LoyaltyRewardSummary
                        {
                            LoyaltyRewardTierId = r.LoyaltyRewardTierId,
                            BusinessId = r.BusinessId,
                            Name = r.Name ?? string.Empty,
                            Description = r.Description,
                            RequiredPoints = r.RequiredPoints,
                            IsActive = r.IsActive,
                            IsSelectable = r.IsSelectable
                        })
                        .ToList();
                }
            }

            var allowedActions =
                value.Mode == DomainLoyaltyScanMode.Accrual
                    ? LoyaltyScanAllowedActions.CanConfirmAccrual
                    : LoyaltyScanAllowedActions.CanConfirmRedemption;

            var response = new ProcessScanSessionForBusinessResponse
            {
                Mode = MapScanMode(value.Mode),
                BusinessId = businessId,
                AccountSummary = new BusinessLoyaltyAccountSummary
                {
                    LoyaltyAccountId = value.LoyaltyAccountId,
                    PointsBalance = value.CurrentPointsBalance,
                    CustomerDisplayName = value.CustomerDisplayName
                },
                SelectedRewards = selectedRewards,
                AllowedActions = allowedActions
            };

            return Ok(response);
        }

        #endregion







        /// <summary>
        /// Attempts to resolve the current business identifier from the
        /// authenticated user principal.
        /// </summary>
        /// <param name="businessId">
        /// When this method returns <c>true</c>, contains the resolved business id.
        /// Otherwise, contains <see cref="Guid.Empty"/>.
        /// </param>
        /// <param name="errorResult">
        /// When this method returns <c>false</c>, contains an appropriate
        /// <see cref="IActionResult"/> that should be returned to the client
        /// (for example <see cref="ForbidResult"/>). When the method returns
        /// <c>true</c>, this value is <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if a valid business identifier could be resolved from the
        /// current principal; otherwise <c>false</c>.
        /// </returns>
        private bool TryGetCurrentBusinessId(out Guid businessId, out IActionResult? errorResult)
        {
            businessId = Guid.Empty;
            errorResult = null;

            // NOTE:
            // The claim type "business_id" must match what JwtTokenService (or any
            // upstream identity provider) emits for business accounts. If this
            // ever changes, adjust the claim type here accordingly.
            var claimValue = User?.FindFirst("business_id")?.Value;

            if (string.IsNullOrWhiteSpace(claimValue) || !Guid.TryParse(claimValue, out businessId))
            {
                // The user is authenticated (we are inside an [Authorize] context)
                // but either not a business user or the token is misconfigured.
                // We return 403 rather than 401 to reflect that distinction.
                errorResult = Forbid();
                businessId = Guid.Empty;
                return false;
            }

            return true;
        }


        /// <summary>
        /// Confirms an accrual operation for a previously prepared scan session
        /// on the business device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The business app calls this endpoint after scanning the consumer's QR
        /// code and collecting the number of points to accrue (for example one
        /// point per visit or a value computed on the device).
        /// </para>
        /// <para>
        /// The business identifier is resolved from the authenticated principal
        /// (for example from a <c>"business_id"</c> claim) and is not taken from
        /// the request body, which prevents tampering with the target business.
        /// </para>
        /// <para>
        /// On success, the endpoint returns a <see cref="ConfirmAccrualResponse"/>
        /// with <see cref="ConfirmAccrualResponse.Success"/> set to <c>true</c>
        /// and the new points balance. In case of validation or business rule
        /// failures, it returns <c>400 Bad Request</c> with a simple error
        /// payload rather than a <see cref="ConfirmAccrualResponse"/>.
        /// </para>
        /// </remarks>
        /// <param name="request">
        /// The accrual confirmation request payload sent by the business app.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ConfirmAccrualResponse"/> on success; otherwise a
        /// <c>400 Bad Request</c> or <c>403 Forbidden</c> result.
        /// </returns>
        [HttpPost("scan/confirm-accrual")]
        [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
        [ProducesResponseType(typeof(ConfirmAccrualResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ConfirmAccrualAsync(
            [FromBody] ConfirmAccrualRequest? request,
            CancellationToken ct = default)
        {
            if (request is null)
            {
                return BadRequestError("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ScanSessionToken))
            {
                return BadRequestError("ScanSessionToken is required.");
            }

            // Keep controller validation format-level only; semantic validation is in Application (resolver/handler).
            if (request.ScanSessionToken.Length > 4000)
            {
                return BadRequestError("ScanSessionToken is too long.");
            }

            if (request.Points <= 0)
            {
                return BadRequestError("Points must be greater than zero.");
            }

            if (!TryGetCurrentBusinessId(out var businessId, out var errorResult))
            {
                return errorResult ?? Forbid();
            }

            var dto = new ConfirmAccrualFromSessionDto
            {
                ScanSessionToken = request.ScanSessionToken,
                Points = request.Points,
                Note = request.Note
            };

            var result = await _confirmAccrualFromSessionHandler
                .HandleAsync(dto, businessId, ct)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var value = result.Value;

            var response = new ConfirmAccrualResponse
            {
                Success = true,
                NewBalance = value.NewPointsBalance,
                UpdatedAccount = null,
                ErrorCode = null,
                ErrorMessage = null
            };

            return Ok(response);
        }




        /// <summary>
        /// Confirms a redemption operation for a previously prepared scan session
        /// on the business device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The business app calls this endpoint after scanning the consumer's QR code
        /// when the scan mode is redemption. The underlying application handler validates
        /// the scan session, ensures it belongs to the current business, and applies
        /// the redemption to the customer's loyalty account.
        /// </para>
        /// <para>
        /// The business identifier is resolved from the authenticated principal
        /// (for example from a <c>"business_id"</c> claim) and is not taken from the
        /// request body to prevent tampering with the target business.
        /// </para>
        /// </remarks>
        /// <param name="request">
        /// The redemption confirmation request payload containing the scan session id.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ConfirmRedemptionResponse"/> on success; otherwise a
        /// <c>400 Bad Request</c> result with a simple error payload.
        /// </returns>
        [HttpPost("scan/confirm-redemption")]
        [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
        [ProducesResponseType(typeof(ConfirmRedemptionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ConfirmRedemptionAsync(
            [FromBody] ConfirmRedemptionRequest? request,
            CancellationToken ct = default)
        {
            if (request is null)
            {
                return BadRequestError("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ScanSessionToken))
            {
                return BadRequestError("ScanSessionToken is required.");
            }

            // Keep controller validation format-level only; semantic validation is in Application (resolver/handler).
            if (request.ScanSessionToken.Length > 4000)
            {
                return BadRequestError("ScanSessionToken is too long.");
            }

            if (!TryGetCurrentBusinessId(out var businessId, out var errorResult))
            {
                return errorResult ?? Forbid();
            }

            var dto = new ConfirmRedemptionFromSessionDto
            {
                ScanSessionToken = request.ScanSessionToken
            };

            var result = await _confirmRedemptionFromSessionHandler
                .HandleAsync(dto, businessId, ct)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var value = result.Value;

            var response = new ConfirmRedemptionResponse
            {
                Success = true,
                NewBalance = value.NewPointsBalance,
                UpdatedAccount = null,
                ErrorCode = null,
                ErrorMessage = null
            };

            return Ok(response);
        }






        /// <summary>
        /// Returns all loyalty accounts for the current authenticated consumer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This endpoint is used by the consumer mobile application to populate
        /// a "My loyalty accounts" screen. It returns one entry per business
        /// where the current user has an active loyalty account.
        /// </para>
        /// <para>
        /// The underlying query handler uses <see cref="ICurrentUserService"/>
        /// to resolve the current user identifier and joins the loyalty account
        /// with the <c>Business</c> entity to obtain a human-friendly name.
        /// </para>
        /// </remarks>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A list of <see cref="LoyaltyAccountSummary"/> items for the current user.
        /// </returns>
        [HttpGet("my/accounts")]
        [Authorize(Policy = "perm:AccessMemberArea")]
        [ProducesResponseType(typeof(IReadOnlyList<LoyaltyAccountSummary>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyAccountsAsync(
            CancellationToken ct = default)
        {
            var result = await _getMyLoyaltyAccountsHandler
                .HandleAsync(ct)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                // Surface application-level failures as a 400 response with a simple
                // { error = "..." } payload, consistent with the Result<T> pattern.
                return ProblemFromResult(result);
            }

            // Map Application DTOs to the public contract model. The contract type
            // is intentionally smaller than the internal DTO; we only expose fields
            // that are required by the mobile UI and keep the rest internal.
            var items = result.Value
                .Select(a => new LoyaltyAccountSummary
                {
                    BusinessId = a.BusinessId,
                    // The contract requires a non-null business name. When the DTO
                    // happens to have a null value (for example, if the business
                    // record was partially populated), we fall back to an empty
                    // string to avoid null reference issues on the client.
                    BusinessName = a.BusinessName ?? string.Empty,
                    PointsBalance = a.PointsBalance,
                    LastAccrualAtUtc = a.LastAccrualAtUtc,
                    // The Application layer does not currently compute the next
                    // reward title for each account. We leave this as null so
                    // clients can optionally hide the field or derive it from
                    // other API calls (e.g., reward tier listings).
                    NextRewardTitle = null
                })
                .ToList();

            return Ok(items);
        }


        /// <summary>
        /// Returns the loyalty points transaction history for the current user
        /// and the specified business.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This endpoint is used by the consumer mobile application to show a
        /// chronological list of accruals, redemptions and manual adjustments
        /// for a single business. The current user is resolved by the application
        /// layer via <c>ICurrentUserService</c>; the business identifier is
        /// provided explicitly as a route parameter.
        /// </para>
        /// <para>
        /// The underlying <see cref="GetMyLoyaltyHistoryHandler"/> returns
        /// <see cref="LoyaltyPointsTransactionDto"/> items, which are mapped to
        /// the public <see cref="PointsTransaction"/> contract type.
        /// </para>
        /// </remarks>
        /// <param name="businessId">
        /// The identifier of the business whose loyalty history should be returned.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A list of <see cref="PointsTransaction"/> entries ordered by newest first.
        /// </returns>
        [HttpGet("my/history/{businessId:guid}")]
        [Authorize(Policy = "perm:AccessMemberArea")]
        [ProducesResponseType(typeof(IReadOnlyList<PointsTransaction>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMyHistoryAsync(
            Guid businessId,
            CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return BadRequestError("BusinessId is required.");
            }

            var result = await _getMyLoyaltyHistoryHandler
                .HandleAsync(businessId, ct)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                // Application-level failures (for example, account not found)
                // are surfaced as a 400 response with a simple { error = "..." }
                // payload via the shared Result<T> helper.
                return ProblemFromResult(result);
            }

            var items = result.Value
                .Select(tx => new PointsTransaction
                {
                    // The contract uses "OccurredAtUtc" while the DTO exposes
                    // "CreatedAtUtc"; conceptually they represent the same timestamp.
                    OccurredAtUtc = tx.CreatedAtUtc,
                    // The contract expects a string type; we use the enum name
                    // (Accrual, Redemption, Adjustment) as a stable machine-readable
                    // value that can also be shown in the UI if needed.
                    Type = tx.Type.ToString(),
                    // The contract calls this field "Delta"; in the DTO it is
                    // named "PointsDelta".
                    Delta = tx.PointsDelta,
                    Reference = tx.Reference,
                    Notes = tx.Notes
                })
                .ToList();

            return Ok(items);
        }


        /// <summary>
        /// Gets a loyalty account summary for the current consumer user within the
        /// specified business context.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This endpoint is designed for the consumer mobile application. It relies on
        /// JWT authentication and the application-layer
        /// <see cref="GetMyLoyaltyAccountForBusinessHandler"/> to resolve the current
        /// user from <c>ICurrentUserService</c>.
        /// </para>
        /// <para>
        /// When no loyalty account exists yet for the current consumer at the given
        /// business, the API returns HTTP 404. Validation or business rule failures
        /// are translated into RFC 7807 problem responses via the shared
        /// <c>Result&lt;T&gt;</c> pattern.
        /// </para>
        /// </remarks>
        /// <param name="businessId">The business identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// HTTP 200 with a <see cref="LoyaltyAccountSummary"/> payload, HTTP 404 when
        /// no account exists, or HTTP 400 with a problem response for validation errors.
        /// </returns>
        [HttpGet("account/{businessId:guid}")]
        [Authorize(Policy = "perm:AccessMemberArea")]
        [ProducesResponseType(typeof(LoyaltyAccountSummary), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentAccountForBusinessAsync(
            Guid businessId,
            CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return BadRequestError("BusinessId is required.");
            }

            // Application handler resolves the current user via ICurrentUserService
            // and returns a Result<LoyaltyAccountSummaryDto?> that captures both
            // validation errors and the not-found case.
            var result = await _getMyLoyaltyAccountForBusinessHandler
                .HandleAsync(businessId, ct)
                .ConfigureAwait(false);

            if (!result.Succeeded)
            {
                // Convert validation/business errors into a problem details response.
                return ProblemFromResult(result);
            }

            var dto = result.Value;
            if (dto is null)
            {
                // No loyalty account exists yet for this (business, user) pair.
                return NotFound("Loyalty account not found for the specified business and user.");
            }

            var response = new LoyaltyAccountSummary
            {
                BusinessId = dto.BusinessId,
                BusinessName = dto.BusinessName ?? string.Empty,
                PointsBalance = dto.PointsBalance,
                LastAccrualAtUtc = dto.LastAccrualAtUtc,
                // LoyaltyAccountSummaryDto currently does not compute the next reward
                // title; the client can derive this using reward-tier APIs if needed.
                NextRewardTitle = null
            };

            return Ok(response);
        }





        /// <summary>
        /// Lists loyalty rewards available for the specified business, taking
        /// the current consumer's loyalty account balance into account.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This endpoint is primarily used by the consumer-facing application to
        /// populate the "available rewards" screen before a scan session is prepared.
        /// It combines the active loyalty program configuration of the business with
        /// the current user's points balance to determine which rewards are
        /// currently selectable.
        /// </para>
        /// <para>
        /// The business identifier is provided explicitly as a route parameter, while
        /// the current user is resolved by the application layer via
        /// <c>ICurrentUserService</c>.
        /// </para>
        /// </remarks>
        /// <param name="businessId">
        /// The identifier of the business whose rewards should be returned.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A list of <see cref="LoyaltyRewardSummary"/> entries that can be displayed
        /// in the client UI.
        /// </returns>
        [HttpGet("business/{businessId:guid}/rewards")]
        [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
        [ProducesResponseType(typeof(IReadOnlyList<LoyaltyRewardSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetRewardsForBusinessAsync(
            Guid businessId,
            CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return BadRequestError("BusinessId is required.");
            }

            var result = await _getAvailableLoyaltyRewardsForBusinessHandler
                .HandleAsync(businessId, ct)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                // Application-level failures (for example, the business does not have
                // an active loyalty program) are surfaced as a 400 response with a
                // simple { error = "..." } payload, consistent with the Result<T>
                // pattern used across the solution.
                return ProblemFromResult(result);
            }

            var rewards = result.Value
                .Select(r => new LoyaltyRewardSummary
                {
                    LoyaltyRewardTierId = r.LoyaltyRewardTierId,
                    BusinessId = r.BusinessId,
                    Name = r.Name ?? string.Empty,
                    Description = r.Description,
                    RequiredPoints = r.RequiredPoints,
                    IsActive = r.IsActive,
                    // The Application DTO also exposes RequiresConfirmation, but the
                    // public contract type intentionally does not. Client applications
                    // that need this information should use separate endpoints or
                    // configuration APIs. We therefore ignore RequiresConfirmation
                    // when mapping to the contract.
                    IsSelectable = r.IsSelectable
                })
                .ToList();

            return Ok(rewards);
        }



    }
}
