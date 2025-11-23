using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.Common;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Shared.Security;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Shared.Security;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Issues a short-lived QR token for the current user (business app).
    /// Stores only a hash of the token in the database.
    /// </summary>
    public sealed class IssueQrCodeHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IClock _clock;

        public IssueQrCodeHandler(IAppDbContext db, ICurrentUserService currentUser, IClock clock)
        {
            _db = db;
            _currentUser = currentUser;
            _clock = clock;
        }

        /// <summary>
        /// Issues a QR code token for the current user.
        /// Expiry duration is currently hard-coded to 2 minutes; can be made configurable via SiteSetting later.
        /// </summary>
        public async Task<QrCodeIssueDto> HandleAsync(CancellationToken ct = default)
        {
            var userId = _currentUser.UserId;
            if (userId == Guid.Empty)
                throw new InvalidOperationException("Current user is not available.");

            // Generate secure random token.
            var rawToken = RandomTokenGenerator.GenerateUrlSafe(32);
            var tokenHash = QrTokenHasher.Hash(rawToken);

            var now = _clock.UtcNow;
            var expires = now.AddMinutes(2);

            // Optional cleanup: mark previous not-yet-consumed tokens as deleted to avoid bloat.
            var staleTokens = await _db.Set<QrCodeToken>()
                .Where(x => x.UserId == userId && !x.IsConsumed && x.ExpiresAtUtc > now)
                .ToListAsync(ct);

            foreach (var t in staleTokens)
            {
                t.IsConsumed = true;
                t.ConsumedAtUtc = now;
            }

            var entity = new QrCodeToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = tokenHash,
                IssuedAtUtc = now,
                ExpiresAtUtc = expires,
                IsConsumed = false,
                Nonce = Guid.NewGuid().ToString("N")
            };

            _db.Set<QrCodeToken>().Add(entity);
            await _db.SaveChangesAsync(ct);

            return new QrCodeIssueDto
            {
                Token = rawToken,
                ExpiresAtUtc = expires
            };
        }
    }
}
