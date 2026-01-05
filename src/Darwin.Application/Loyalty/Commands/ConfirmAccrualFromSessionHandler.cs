using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Services;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Confirms an accrual operation for a previously prepared scan session identified by an opaque token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler is invoked by the business app after scanning the consumer's QR code.
    /// The QR payload contains only an opaque <c>ScanSessionToken</c> (string) which is resolved
    /// to an internal <see cref="ScanSession"/> through <see cref="ScanSessionTokenResolver"/>.
    /// </para>
    /// <para>
    /// The handler enforces one-time use semantics by consuming the underlying QR token and by
    /// transitioning the scan session from <see cref="LoyaltyScanStatus.Pending"/> to
    /// <see cref="LoyaltyScanStatus.Completed"/> on success.
    /// </para>
    /// <para>
    /// All semantic validation (expiry, ownership, status machine) lives in the handler / resolver.
    /// Input validators must remain persistence-free and check only basic invariants (non-empty token, etc.).
    /// </para>
    /// </remarks>
    public sealed class ConfirmAccrualFromSessionHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUserService;
        private readonly IClock _clock;
        private readonly ScanSessionTokenResolver _tokenResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmAccrualFromSessionHandler"/> class.
        /// </summary>
        public ConfirmAccrualFromSessionHandler(
            IAppDbContext db,
            ICurrentUserService currentUserService,
            IClock clock,
            ScanSessionTokenResolver tokenResolver)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _tokenResolver = tokenResolver ?? throw new ArgumentNullException(nameof(tokenResolver));
        }

        /// <summary>
        /// Confirms an accrual operation for the specified scan session token within the given business context.
        /// </summary>
        /// <param name="dto">Request payload containing the token and the number of points to add.</param>
        /// <param name="businessId">Business identifier resolved by WebApi from the authenticated business context.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Result<ConfirmAccrualResultDto>> HandleAsync(
            ConfirmAccrualFromSessionDto dto,
            Guid businessId,
            CancellationToken ct = default)
        {
            if (dto is null)
            {
                return Result<ConfirmAccrualResultDto>.Fail("Request is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.ScanSessionToken))
            {
                return Result<ConfirmAccrualResultDto>.Fail("ScanSessionToken is required.");
            }

            if (businessId == Guid.Empty)
            {
                return Result<ConfirmAccrualResultDto>.Fail("BusinessId is required.");
            }

            if (dto.Points <= 0)
            {
                return Result<ConfirmAccrualResultDto>.Fail("Points must be a positive integer.");
            }

            var resolvedResult = await _tokenResolver
                .ResolveForBusinessAsync(dto.ScanSessionToken, businessId, ct)
                .ConfigureAwait(false);

            if (!resolvedResult.Succeeded || resolvedResult.Value is null)
            {
                return Result<ConfirmAccrualResultDto>.Fail(resolvedResult.Error ?? "Invalid scan session token.");
            }

            var token = resolvedResult.Value.Token;
            var session = resolvedResult.Value.Session;

            if (session.Mode != LoyaltyScanMode.Accrual)
            {
                return Result<ConfirmAccrualResultDto>.Fail("Scan session is not in accrual mode.");
            }

            // Consume token early to reduce replay window.
            await _tokenResolver
                .ConsumeAsync(token, businessId, session.BusinessLocationId, ct)
                .ConfigureAwait(false);

            // -----------------------------------------------------------------
            // Concurrency re-check:
            // If a competing request consumed the token first, ConsumeAsync is a no-op.
            // We detect that here and stop the operation.
            // -----------------------------------------------------------------
            var tokenAfterConsume = await _db.Set<QrCodeToken>()
                .AsNoTracking()
                .SingleOrDefaultAsync(t => t.Id == token.Id && !t.IsDeleted, ct)
                .ConfigureAwait(false);

            if (tokenAfterConsume is null ||
                tokenAfterConsume.ConsumedAtUtc is null ||
                tokenAfterConsume.ConsumedByBusinessId != businessId)
            {
                // Best-effort: mark the session as cancelled to aid debugging and analytics.
                session.Status = LoyaltyScanStatus.Cancelled;
                session.Outcome = "TokenAlreadyConsumed";
                session.FailureReason = "Token was consumed concurrently by another request.";
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                return Result<ConfirmAccrualResultDto>.Fail("Scan session token has already been consumed.");
            }

            var account = await _db.Set<LoyaltyAccount>()
                .AsQueryable()
                .SingleOrDefaultAsync(a => a.Id == session.LoyaltyAccountId && !a.IsDeleted, ct)
                .ConfigureAwait(false);

            if (account is null)
            {
                session.Status = LoyaltyScanStatus.Cancelled;
                session.Outcome = "AccountNotFound";
                session.FailureReason = "Loyalty account for scan session not found.";
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                return Result<ConfirmAccrualResultDto>.Fail("Loyalty account for scan session not found.");
            }

            if (account.Status != LoyaltyAccountStatus.Active)
            {
                session.Status = LoyaltyScanStatus.Cancelled;
                session.Outcome = "AccountNotActive";
                session.FailureReason = "Loyalty account is not active.";
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                return Result<ConfirmAccrualResultDto>.Fail("Loyalty account is not active.");
            }

            var now = _clock.UtcNow;

            if (session.ExpiresAtUtc <= now)
            {
                session.Status = LoyaltyScanStatus.Expired;
                session.Outcome = "Expired";
                session.FailureReason = "Session expired before accrual confirmation.";
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                return Result<ConfirmAccrualResultDto>.Fail("Scan session has expired.");
            }

            var staffUserId = _currentUserService.GetCurrentUserId();
            if (staffUserId == Guid.Empty)
            {
                return Result<ConfirmAccrualResultDto>.Fail("User is not authenticated.");
            }

            var transaction = new LoyaltyPointsTransaction
            {
                LoyaltyAccountId = account.Id,
                BusinessId = session.BusinessId,
                Type = LoyaltyPointsTransactionType.Accrual,
                PointsDelta = dto.Points,
                RewardRedemptionId = null,
                BusinessLocationId = session.BusinessLocationId,
                PerformedByUserId = staffUserId,
                Reference = null,
                Notes = dto.Note
            };

            _db.Set<LoyaltyPointsTransaction>().Add(transaction);

            account.PointsBalance += dto.Points;
            account.LifetimePoints += dto.Points;
            account.LastAccrualAtUtc = now;

            session.Status = LoyaltyScanStatus.Completed;
            session.Outcome = "Accrued";
            session.FailureReason = null;
            session.ResultingTransactionId = transaction.Id;

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            var result = new ConfirmAccrualResultDto
            {
                LoyaltyAccountId = account.Id,
                NewPointsBalance = account.PointsBalance,
                NewLifetimePoints = account.LifetimePoints,
                PointsTransactionId = transaction.Id
            };

            return Result<ConfirmAccrualResultDto>.Ok(result);
        }
    }
}
