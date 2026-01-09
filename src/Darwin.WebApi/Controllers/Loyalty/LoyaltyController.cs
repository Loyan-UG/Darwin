using Darwin.Application.Loyalty.Commands;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Queries;
using Darwin.Contracts.Common;
using Darwin.Contracts.Loyalty;
using Darwin.Shared.Results;
using Darwin.WebApi.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContractLoyaltyScanMode = Darwin.Contracts.Loyalty.LoyaltyScanMode;
// Use explicit aliases to avoid ambiguity between domain and contract enums.
using DomainLoyaltyScanMode = Darwin.Domain.Enums.LoyaltyScanMode;

namespace Darwin.WebApi.Controllers.Loyalty
{
    /// <summary>
    /// API endpoints for the loyalty system used by both consumer and business mobile applications.
    /// </summary>
    /// <remarks>
    /// IMPORTANT:
    /// This controller must remain thin. It performs:
    /// - Request validation (format-level)
    /// - Auth/claims boundary checks
    /// - Delegation to Application handlers
    /// - Mapping from Application DTOs to Darwin.Contracts
    /// 
    /// All business rules must remain in the Application layer.
    /// </remarks>
    [ApiController]
    [Route("api/v1/loyalty")]
    [Authorize]
    public sealed class LoyaltyController : ApiControllerBase
    {
        private readonly PrepareScanSessionHandler _prepareScanSessionHandler;
        private readonly ProcessScanSessionForBusinessHandler _processScanSessionForBusinessHandler;
        private readonly ConfirmAccrualFromSessionHandler _confirmAccrualFromSessionHandler;
        private readonly ConfirmRedemptionFromSessionHandler _confirmRedemptionFromSessionHandler;
        private readonly GetMyLoyaltyAccountsHandler _getMyLoyaltyAccountsHandler;
        private readonly GetMyLoyaltyHistoryHandler _getMyLoyaltyHistoryHandler;
        private readonly GetMyLoyaltyAccountForBusinessHandler _getMyLoyaltyAccountForBusinessHandler;
        private readonly GetAvailableLoyaltyRewardsForBusinessHandler _getAvailableLoyaltyRewardsForBusinessHandler;
        private readonly GetMyLoyaltyBusinessesHandler _getMyLoyaltyBusinessesHandler;
        private readonly GetMyLoyaltyTimelinePageHandler _getMyLoyaltyTimelinePageHandler;


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
            GetAvailableLoyaltyRewardsForBusinessHandler getAvailableLoyaltyRewardsForBusinessHandler,
            GetMyLoyaltyBusinessesHandler getMyLoyaltyBusinessesHandler,
            GetMyLoyaltyTimelinePageHandler getMyLoyaltyTimelinePageHandler)
        {
            _prepareScanSessionHandler = prepareScanSessionHandler ?? throw new ArgumentNullException(nameof(prepareScanSessionHandler));
            _processScanSessionForBusinessHandler = processScanSessionForBusinessHandler ?? throw new ArgumentNullException(nameof(processScanSessionForBusinessHandler));
            _confirmAccrualFromSessionHandler = confirmAccrualFromSessionHandler ?? throw new ArgumentNullException(nameof(confirmAccrualFromSessionHandler));
            _confirmRedemptionFromSessionHandler = confirmRedemptionFromSessionHandler ?? throw new ArgumentNullException(nameof(confirmRedemptionFromSessionHandler));
            _getMyLoyaltyAccountsHandler = getMyLoyaltyAccountsHandler ?? throw new ArgumentNullException(nameof(getMyLoyaltyAccountsHandler));
            _getMyLoyaltyHistoryHandler = getMyLoyaltyHistoryHandler ?? throw new ArgumentNullException(nameof(getMyLoyaltyHistoryHandler));
            _getMyLoyaltyAccountForBusinessHandler = getMyLoyaltyAccountForBusinessHandler ?? throw new ArgumentNullException(nameof(getMyLoyaltyAccountForBusinessHandler));
            _getAvailableLoyaltyRewardsForBusinessHandler = getAvailableLoyaltyRewardsForBusinessHandler ?? throw new ArgumentNullException(nameof(getAvailableLoyaltyRewardsForBusinessHandler));
            _getMyLoyaltyBusinessesHandler = getMyLoyaltyBusinessesHandler ?? throw new ArgumentNullException(nameof(getMyLoyaltyBusinessesHandler));
            _getMyLoyaltyTimelinePageHandler = getMyLoyaltyTimelinePageHandler ?? throw new ArgumentNullException(nameof(getMyLoyaltyTimelinePageHandler));
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
        [Authorize(Policy = "perm:AccessMemberArea")]
        [ProducesResponseType(typeof(PrepareScanSessionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PrepareScanSessionAsync(
            [FromBody] PrepareScanSessionRequest? request,
            CancellationToken ct = default)
        {
            if (request is null)
            {
                return BadRequestProblem("Request body is required.");
            }

            if (request.BusinessId == Guid.Empty)
            {
                return BadRequestProblem("BusinessId is required.");
            }

            var dto = new PrepareScanSessionDto
            {
                BusinessId = request.BusinessId,
                BusinessLocationId = request.BusinessLocationId,
                Mode = LoyaltyContractsMapper.ToDomain(request.Mode),
                SelectedRewardTierIds = request.SelectedRewardTierIds?
                    .Where(x => x != Guid.Empty)
                    .Distinct()
                    .ToList() ?? new List<Guid>(),
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

            // Redemption: enrich accepted tier ids with reward details via centralized helper.
            if (value.Mode == Darwin.Domain.Enums.LoyaltyScanMode.Redemption &&
                value.SelectedRewardTierIds is { Count: > 0 })
            {
                // caller decides policy: for prepare (consumer-initiated redemption) we want strict behavior
                var enrichResult = await BuildSelectedRewardsAsync(request.BusinessId, value.SelectedRewardTierIds, failIfMissing: true, ct);
                if (!enrichResult.Succeeded)
                {
                    return ProblemFromResult(enrichResult); // converts Result.Fail -> ProblemDetails
                }
                selectedRewards = enrichResult.Value;
            }

            var response = new PrepareScanSessionResponse
            {
                ScanSessionToken = value.ScanSessionToken,
                Mode = LoyaltyContractsMapper.ToContract(value.Mode),
                ExpiresAtUtc = value.ExpiresAtUtc,
                CurrentPointsBalance = value.CurrentPointsBalance,
                SelectedRewards = selectedRewards
            };

            return Ok(response);
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
                return BadRequestProblem("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ScanSessionToken))
            {
                return BadRequestProblem("ScanSessionToken is required.");
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

            // Redemption: enrich selected tier ids with reward details via centralized helper.
            if (value.Mode == Darwin.Domain.Enums.LoyaltyScanMode.Redemption &&
                value.SelectedRewards is { Count: > 0 })
            {
                var tierIds = value.SelectedRewards?
                    .Select(x => x.LoyaltyRewardTierId)
                    .Where(x => x != Guid.Empty)
                    .Distinct()
                    .ToList();

                var enrichResult = await BuildSelectedRewardsAsync(businessId, tierIds, failIfMissing: true, ct);
                if (!enrichResult.Succeeded)
                    return ProblemFromResult(enrichResult);
                selectedRewards = enrichResult.Value;
            }

            var allowedActions =
                value.Mode == Darwin.Domain.Enums.LoyaltyScanMode.Accrual
                    ? LoyaltyScanAllowedActions.CanConfirmAccrual
                    : LoyaltyScanAllowedActions.CanConfirmRedemption;

            // IMPORTANT:
            // Return the contract type (not an anonymous object) to keep OpenAPI/clients stable.
            var response = new ProcessScanSessionForBusinessResponse
            {
                Mode = LoyaltyContractsMapper.ToContract(value.Mode),
                BusinessId = businessId,
                AccountSummary = LoyaltyContractsMapper.ToContractBusinessAccountSummary(value),
                SelectedRewards = selectedRewards,
                AllowedActions = allowedActions
            };

            return Ok(response);
        }
        #endregion



        /// <summary>
        /// Enriches selected reward tier ids with public reward metadata for the specified business.
        /// Returns Result.Failed when the enrichment could not be performed or when required tier ids were missing
        /// and failIfMissing is true.
        /// </summary>
        /// <param name="businessId">Business to query available rewards for.</param>
        /// <param name="selectedTierIds">Ordered collection of selected reward tier ids (may be null/empty).</param>
        /// <param name="failIfMissing">When true, treat missing reward metadata as an error; otherwise return best-effort list.</param>
        /// <param name="ct">Cancellation token.</param>
        private async Task<Result<IReadOnlyList<LoyaltyRewardSummary>>> BuildSelectedRewardsAsync(
            Guid businessId,
            IReadOnlyCollection<Guid>? selectedTierIds,
            bool failIfMissing,
            CancellationToken ct = default)
        {
            if (selectedTierIds is null || selectedTierIds.Count == 0)
            {
                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(Array.Empty<LoyaltyRewardSummary>());
            }

            // Normalize requested ids, preserve provided order but remove empties/dups.
            var orderedDistinct = selectedTierIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();

            if (orderedDistinct.Count == 0)
            {
                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(Array.Empty<LoyaltyRewardSummary>());
            }

            // Fetch available rewards from Application handler
            var availableRewardsResult = await _getAvailableLoyaltyRewardsForBusinessHandler
                .HandleAsync(businessId, ct)
                .ConfigureAwait(false);

            if (!availableRewardsResult.Succeeded || availableRewardsResult.Value is null)
            {
                if (failIfMissing)
                {
                    return Result<IReadOnlyList<LoyaltyRewardSummary>>.Fail("Could not load business rewards for enrichment.");
                }
                else
                {
                    // Best-effort: return empty list to keep caller available
                    return Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(Array.Empty<LoyaltyRewardSummary>());
                }
            }

            var available = availableRewardsResult.Value;

            // Build a dictionary for fast lookup
            var dict = available.ToDictionary(r => r.LoyaltyRewardTierId);

            var missing = new List<Guid>();
            var resultList = new List<LoyaltyRewardSummary>(orderedDistinct.Count);

            // Preserve the order of orderedDistinct
            foreach (var id in orderedDistinct)
            {
                if (dict.TryGetValue(id, out var rewardDto))
                {
                    resultList.Add(new LoyaltyRewardSummary
                    {
                        LoyaltyRewardTierId = rewardDto.LoyaltyRewardTierId,
                        BusinessId = rewardDto.BusinessId,
                        Name = rewardDto.Name ?? string.Empty,
                        Description = rewardDto.Description,
                        RequiredPoints = rewardDto.RequiredPoints,
                        IsActive = rewardDto.IsActive,
                        IsSelectable = rewardDto.IsSelectable
                    });
                }
                else
                {
                    missing.Add(id);
                }
            }

            if (missing.Count > 0 && failIfMissing)
            {
                // Helpful diagnostic: include count or first missing id (avoid leaking data)
                return Result<IReadOnlyList<LoyaltyRewardSummary>>.Fail("Some selected rewards are not available for this business.");
            }

            return Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(resultList);
        }



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
                return BadRequestProblem("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ScanSessionToken))
            {
                return BadRequestProblem("ScanSessionToken is required.");
            }

            if (request.ScanSessionToken.Length > 4000)
            {
                return BadRequestProblem("ScanSessionToken is too long.");
            }

            if (request.Points <= 0)
            {
                return BadRequestProblem("Points must be greater than zero.");
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

                // Future-friendly: allow richer response later (needs an Application DTO for updated account snapshot)
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
                return BadRequestProblem("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ScanSessionToken))
            {
                return BadRequestProblem("ScanSessionToken is required.");
            }

            if (request.ScanSessionToken.Length > 4000)
            {
                return BadRequestProblem("ScanSessionToken is too long.");
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

            /// FIX: previously mapping omitted LoyaltyAccountId/LifetimePoints/Status.
            // Centralized mapping ensures contract completeness and stability.
            var items = result.Value
                .Select(LoyaltyContractsMapper.ToContract)
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
                return BadRequestProblem("BusinessId is required.");
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
                .Select(LoyaltyContractsMapper.ToContract)
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
                return BadRequestProblem("BusinessId is required.");
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
                return NotFoundProblem("Loyalty account not found for the specified business and user.");
            }

            return Ok(LoyaltyContractsMapper.ToContract(dto));
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
        [Authorize(Policy = "perm:AccessMemberArea")]
        [ProducesResponseType(typeof(IReadOnlyList<LoyaltyRewardSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetRewardsForBusinessAsync(
            Guid businessId,
            CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return BadRequestProblem("BusinessId is required.");
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
                .Select(LoyaltyContractsMapper.ToContract)
                .ToList();

            return Ok(rewards);
        }



        /// <summary>
        /// Returns the list of businesses for which the current user has a loyalty account ("My places").
        /// This endpoint is consumer/member scoped and relies on the current authenticated user resolved
        /// in the Application layer via <c>ICurrentUserService</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The underlying Application handler uses standard joins/subqueries only (DB-agnostic),
        /// and does not expose any internal identifiers.
        /// </para>
        /// <para>
        /// This endpoint uses offset paging because the list is user-scoped and expected to be small.
        /// </para>
        /// </remarks>
        /// <param name="page">1-based page index. Defaults to 1.</param>
        /// <param name="pageSize">Page size. Defaults to 20. Maximum 200.</param>
        /// <param name="includeInactiveBusinesses">
        /// When true, includes businesses that may be inactive/hidden for discovery but still have an account.
        /// </param>
        /// <param name="ct">Cancellation token propagated from the HTTP request.</param>
        /// <returns>A 200 OK with a paged list of "My places" items.</returns>
        [HttpGet("my/businesses")]
        [Authorize(Policy = "perm:AccessMemberArea")]
        [ProducesResponseType(typeof(MyLoyaltyBusinessesResponse), 200)]
        [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), 400)]
        public async Task<IActionResult> GetMyBusinessesAsync(
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] bool? includeInactiveBusinesses,
            CancellationToken ct = default)
        {
            var normalizedPage = page.GetValueOrDefault(1);
            if (normalizedPage <= 0)
            {
                return BadRequestProblem("Page must be a positive integer.");
            }

            var normalizedPageSize = pageSize.GetValueOrDefault(20);
            if (normalizedPageSize <= 0 || normalizedPageSize > 200)
            {
                return BadRequestProblem("PageSize must be between 1 and 200.");
            }

            // IMPORTANT:
            // The Application handler expects MyLoyaltyBusinessListRequestDto (not a GetMy... DTO).
            var request = new MyLoyaltyBusinessListRequestDto
            {
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                IncludeInactiveBusinesses = includeInactiveBusinesses.GetValueOrDefault(false)
            };

            // IMPORTANT:
            // This handler returns a tuple (Items, Total) and is NOT Result-wrapped.
            var (items, total) = await _getMyLoyaltyBusinessesHandler
                .HandleAsync(request, ct)
                .ConfigureAwait(false);

            var safeItems = items ?? new List<MyLoyaltyBusinessListItemDto>();

            var response = new MyLoyaltyBusinessesResponse
            {
                Total = total,
                Items = safeItems.Select(LoyaltyContractsMapper.ToContract).ToList(),
                Request = new PagedRequest
                {
                    Page = normalizedPage,
                    PageSize = normalizedPageSize,
                    Search = null
                }
            };

            return Ok(response);
        }






        /// <summary>
        /// Returns a single page of unified loyalty timeline entries for the current consumer user
        /// within a specific business context.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This endpoint is consumer-facing and is designed for mobile "Activity" / "Timeline" screens.
        /// It returns a unified stream of entries (transactions + redemptions) ordered newest-first.
        /// </para>
        /// <para>
        /// Paging is cursor-based (keyset) using (BeforeAtUtc, BeforeId) to keep it deterministic and
        /// provider-agnostic. The cursor rules are enforced by the Application handler as well.
        /// </para>
        /// <para>
        /// IMPORTANT (Token-First Rule):
        /// This endpoint must not expose internal operational identifiers such as ScanSessionId.
        /// The returned entries contain only user-safe identifiers (transaction ids / redemption ids).
        /// </para>
        /// </remarks>
        /// <param name="request">Paging request (business + cursor).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// HTTP 200 with a timeline page, or HTTP 400 with a problem response when validation fails.
        /// </returns>
        [HttpPost("my/timeline")]
        [Authorize(Policy = "perm:AccessMemberArea")]
        [ProducesResponseType(typeof(GetMyLoyaltyTimelinePageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMyLoyaltyTimelinePageAsync(
            [FromBody] GetMyLoyaltyTimelinePageRequest? request,
            CancellationToken ct = default)
        {
            if (request is null)
            {
                return BadRequestProblem("Request body is required.");
            }

            // Contract currently allows nullable BusinessId; Application requires a non-empty GUID.
            if (!request.BusinessId.HasValue || request.BusinessId.Value == Guid.Empty)
            {
                return BadRequestProblem("BusinessId is required and must be a non-empty GUID.");
            }

            // Defensive cursor validation at API boundary (Application validates again).
            // Cursor correctness: both parts must be provided together.
            if ((request.BeforeAtUtc is null) != (request.BeforeId is null))
            {
                return BadRequestProblem("Invalid cursor. Both BeforeAtUtc and BeforeId must be provided together.");
            }

            var dto = new GetMyLoyaltyTimelinePageDto
            {
                BusinessId = request.BusinessId.Value,
                PageSize = request.PageSize,
                BeforeAtUtc = request.BeforeAtUtc,
                BeforeId = request.BeforeId
            };

            var result = await _getMyLoyaltyTimelinePageHandler
                .HandleAsync(dto, ct)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                return ProblemFromResult(result);
            }

            var value = result.Value;

            var response = new GetMyLoyaltyTimelinePageResponse
            {
                Items = (value.Items ?? Array.Empty<LoyaltyTimelineEntryDto>())
                    .Select(LoyaltyContractsMapper.ToContract)
                    .ToList(),
                NextBeforeAtUtc = value.NextBeforeAtUtc,
                NextBeforeId = value.NextBeforeId
            };

            return Ok(response);
        }


    }
}
