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
    /// Redeems a reward tier by subtracting required points during an active scan session.
    /// </summary>
    public sealed class RedeemRewardHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IClock _clock;

        public RedeemRewardHandler(IAppDbContext db, ICurrentUserService currentUser, IClock clock)
        {
            _db = db;
            _currentUser = currentUser;
            _clock = clock;
        }

        /// <summary>
        /// Performs a redemption and records a transaction.
        /// </summary>
        public async Task<RedeemRewardResponseDto> HandleAsync(RedeemRewardRequestDto request, CancellationToken ct = default)
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

            var tier = await _db.Set<LoyaltyRewardTier>()
                .FirstOrDefaultAsync(x => x.Id == request.RewardTierId && x.BusinessUserId == businessUserId, ct);

            if (tier is null || !tier.IsActive)
                throw new InvalidOperationException("Reward tier is not available.");

            var account = await _db.Set<LoyaltyAccount>()
                .FirstOrDefaultAsync(x =>
                    x.BusinessUserId == businessUserId &&
                    x.ConsumerUserId == session.ConsumerUserId, ct);

            if (account is null)
                throw new InvalidOperationException("Loyalty account was not found.");

            if (account.PointsBalance < tier.RequiredPoints)
                throw new InvalidOperationException("Insufficient points.");

            account.PointsBalance -= tier.RequiredPoints;
            account.TotalPointsRedeemed += tier.RequiredPoints;
            account.LastActivityAtUtc = now;

            var tx = new LoyaltyPointTransaction
            {
                Id = Guid.NewGuid(),
                LoyaltyAccountId = account.Id,
                BusinessUserId = businessUserId,
                ConsumerUserId = session.ConsumerUserId,
                PointsDelta = -tier.RequiredPoints,
                Type = LoyaltyPointTransactionType.Redemption,
                Note = request.Note ?? tier.Title,
                OccurredAtUtc = now,
                ScanSessionId = session.Id,
                RewardTierId = tier.Id
            };

            _db.Set<LoyaltyPointTransaction>().Add(tx);
            await _db.SaveChangesAsync(ct);

            return new RedeemRewardResponseDto
            {
                LoyaltyAccountId = account.Id,
                NewPointsBalance = account.PointsBalance,
                RedemptionTransactionId = tx.Id
            };
        }
    }
}
