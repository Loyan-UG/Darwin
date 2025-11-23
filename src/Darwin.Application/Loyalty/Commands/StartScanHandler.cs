using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.Common;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Loyalty;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Starts a scan session for a business user after they scan a consumer's QR token.
    /// </summary>
    public sealed class StartScanHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IClock _clock;

        public StartScanHandler(IAppDbContext db, ICurrentUserService currentUser, IClock clock)
        {
            _db = db;
            _currentUser = currentUser;
            _clock = clock;
        }

        /// <summary>
        /// Validates QR token, marks it consumed, creates a ScanSession, and returns session info.
        /// </summary>
        public async Task<StartScanResponseDto> HandleAsync(StartScanRequestDto request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var businessUserId = _currentUser.UserId;
            if (businessUserId == Guid.Empty)
                throw new InvalidOperationException("Current user is not available.");

            var now = _clock.UtcNow;
            var tokenHash = QrTokenHasher.Hash(request.Token);

            var token = await _db.Set<QrCodeToken>()
                .FirstOrDefaultAsync(x =>
                    x.TokenHash == tokenHash &&
                    !x.IsConsumed &&
                    x.ExpiresAtUtc > now, ct);

            if (token is null)
                throw new InvalidOperationException("QR token is invalid or expired.");

            // Prevent replay.
            token.IsConsumed = true;
            token.ConsumedAtUtc = now;

            var consumerUserId = token.UserId;

            // Ensure account exists.
            var account = await _db.Set<LoyaltyAccount>()
                .FirstOrDefaultAsync(x => x.BusinessUserId == businessUserId && x.ConsumerUserId == consumerUserId, ct);

            if (account is null)
            {
                account = new LoyaltyAccount
                {
                    Id = Guid.NewGuid(),
                    BusinessUserId = businessUserId,
                    ConsumerUserId = consumerUserId,
                    PointsBalance = 0,
                    TotalPointsEarned = 0,
                    TotalPointsRedeemed = 0,
                    LastActivityAtUtc = now
                };
                _db.Set<LoyaltyAccount>().Add(account);
            }

            var session = new ScanSession
            {
                Id = Guid.NewGuid(),
                BusinessUserId = businessUserId,
                ConsumerUserId = consumerUserId,
                StartedAtUtc = now,
                ExpiresAtUtc = now.AddMinutes(5),
                IsClosed = false
            };

            _db.Set<ScanSession>().Add(session);
            await _db.SaveChangesAsync(ct);

            return new StartScanResponseDto
            {
                ScanSessionId = session.Id,
                BusinessUserId = businessUserId,
                ConsumerUserId = consumerUserId,
                CurrentPointsBalance = account.PointsBalance
            };
        }
    }
}
