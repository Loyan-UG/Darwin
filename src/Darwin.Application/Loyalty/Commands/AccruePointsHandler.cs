using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Adds points to a consumer during an active scan session.
    /// </summary>
    public sealed class AccruePointsHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IClock _clock;

        public AccruePointsHandler(IAppDbContext db, ICurrentUserService currentUser, IClock clock)
        {
            _db = db;
            _currentUser = currentUser;
            _clock = clock;
        }

        /// <summary>
        /// Accrues points for the consumer and records a transaction.
        /// </summary>
        public async Task<AccruePointsResponseDto> HandleAsync(AccruePointsRequestDto request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var businessUserId = _currentUser.UserId;
            if (businessUserId == Guid.Empty)
                throw new InvalidOperationException("Current user is not available.");

            var now = _clock.UtcNow;

            var session = await _db.Set<ScanSession>()
                .FirstOrDefaultAsync(x =>
                    x.Id == request.ScanSessionId &&
                    x.BusinessUserId == businessUserId &&
                    !x.IsClosed &&
                    x.ExpiresAtUtc > now, ct);

            if (session is null)
                throw new InvalidOperationException("Scan session is invalid or expired.");

            var account = await _db.Set<LoyaltyAccount>()
                .FirstOrDefaultAsync(x =>
                    x.BusinessUserId == businessUserId &&
                    x.ConsumerUserId == session.ConsumerUserId, ct);

            if (account is null)
                throw new InvalidOperationException("Loyalty account was not found.");

            account.PointsBalance += request.PointsToAdd;
            account.TotalPointsEarned += request.PointsToAdd;
            account.LastActivityAtUtc = now;

            var tx = new LoyaltyPointTransaction
            {
                Id = Guid.NewGuid(),
                LoyaltyAccountId = account.Id,
                BusinessUserId = businessUserId,
                ConsumerUserId = session.ConsumerUserId,
                PointsDelta = request.PointsToAdd,
                Type = LoyaltyPointTransactionType.Accrual,
                Note = request.Note,
                OccurredAtUtc = now,
                ScanSessionId = session.Id
            };

            _db.Set<LoyaltyPointTransaction>().Add(tx);
            await _db.SaveChangesAsync(ct);

            return new AccruePointsResponseDto
            {
                LoyaltyAccountId = account.Id,
                NewPointsBalance = account.PointsBalance,
                TransactionId = tx.Id
            };
        }
    }
}
