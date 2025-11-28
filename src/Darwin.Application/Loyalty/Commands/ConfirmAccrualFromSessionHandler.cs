using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Confirms an accrual operation for a previously prepared scan session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler is invoked by the business app after scanning the consumer's QR
    /// and collecting the accrual amount (for example, one point per visit or an
    /// amount-based point calculation performed in the UI).
    /// </para>
    /// <para>
    /// The handler validates that the scan session belongs to the specified business,
    /// is in accrual mode, has not expired and is still pending. It then creates a
    /// <see cref="LoyaltyPointsTransaction"/> entry, updates the loyalty account
    /// balance and lifetime points, and marks the scan session as completed.
    /// </para>
    /// <para>
    /// The session is one-time use. Any subsequent attempt to confirm the same
    /// session will fail because the status is no longer <see cref="LoyaltyScanStatus.Pending"/>.
    /// </para>
    /// </remarks>
    public sealed class ConfirmAccrualFromSessionHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUserService;
        private readonly IClock _clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmAccrualFromSessionHandler"/> class.
        /// </summary>
        /// <param name="db">Application database context abstraction.</param>
        /// <param name="currentUserService">Provides the user id of the business staff member.</param>
        /// <param name="clock">Time abstraction used to obtain UTC timestamps.</param>
        public ConfirmAccrualFromSessionHandler(
            IAppDbContext db,
            ICurrentUserService currentUserService,
            IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Confirms an accrual operation for the specified scan session.
        /// </summary>
        /// <param name="dto">
        /// Data describing the scan session and the number of points to accrue.
        /// </param>
        /// <param name="businessId">
        /// Identifier of the business from the authenticated context of the
        /// business app. The handler verifies that the scan session belongs
        /// to this business.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="Result{T}"/> containing <see cref="ConfirmAccrualResultDto"/>
        /// on success or an error message on failure.
        /// </returns>
        public async Task<Result<ConfirmAccrualResultDto>> HandleAsync(
            ConfirmAccrualFromSessionDto dto,
            Guid businessId,
            CancellationToken ct = default)
        {
            if (dto.ScanSessionId == Guid.Empty)
            {
                return Result<ConfirmAccrualResultDto>.Fail("ScanSessionId is required.");
            }

            if (businessId == Guid.Empty)
            {
                return Result<ConfirmAccrualResultDto>.Fail("BusinessId is required.");
            }

            if (dto.Points <= 0)
            {
                return Result<ConfirmAccrualResultDto>.Fail("Points must be a positive integer.");
            }

            var session = await _db.Set<ScanSession>()
                .AsQueryable()
                .SingleOrDefaultAsync(
                    s => s.Id == dto.ScanSessionId && !s.IsDeleted,
                    ct)
                .ConfigureAwait(false);

            if (session is null)
            {
                return Result<ConfirmAccrualResultDto>.Fail("Scan session not found.");
            }

            if (session.BusinessId != businessId)
            {
                return Result<ConfirmAccrualResultDto>.Fail("Scan session does not belong to this business.");
            }

            if (session.Mode != LoyaltyScanMode.Accrual)
            {
                return Result<ConfirmAccrualResultDto>.Fail("Scan session is not in accrual mode.");
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

            if (session.Status != LoyaltyScanStatus.Pending)
            {
                return Result<ConfirmAccrualResultDto>.Fail("Scan session is not in a pending state.");
            }

            var account = await _db.Set<LoyaltyAccount>()
                .AsQueryable()
                .SingleOrDefaultAsync(
                    a => a.Id == session.LoyaltyAccountId && !a.IsDeleted,
                    ct)
                .ConfigureAwait(false);

            if (account is null)
            {
                return Result<ConfirmAccrualResultDto>.Fail("Loyalty account for scan session not found.");
            }

            if (account.Status != LoyaltyAccountStatus.Active)
            {
                return Result<ConfirmAccrualResultDto>.Fail("Loyalty account is not active.");
            }

            var staffUserId = _currentUserService.GetCurrentUserId();

            // Create the ledger entry for the accrual.
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

            // Update the account balance and lifetime points.
            account.PointsBalance += dto.Points;
            account.LifetimePoints += dto.Points;
            account.LastAccrualAtUtc = now;

            // Mark the session as completed and link the resulting transaction.
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
