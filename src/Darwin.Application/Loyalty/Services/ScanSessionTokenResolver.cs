using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Services
{
    /// <summary>
    /// Centralizes resolution and validation of scan session tokens (opaque string)
    /// to their underlying <see cref="QrCodeToken"/> and <see cref="ScanSession"/> records.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service exists to enforce the "token-first" boundary: external layers
    /// must never resolve or work with internal scan session identifiers.
    /// </para>
    /// <para>
    /// Validation responsibilities include:
    /// expiry, one-time-use consumption, business binding, and scan session state.
    /// </para>
    /// </remarks>
    public sealed class ScanSessionTokenResolver
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScanSessionTokenResolver"/> class.
        /// </summary>
        public ScanSessionTokenResolver(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Resolves a token to its underlying records and validates that it can be used
        /// by the specified business at this point in time.
        /// </summary>
        /// <param name="scanSessionToken">Opaque scan token from the QR payload.</param>
        /// <param name="businessId">Business id from authenticated context.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A successful result containing the token and session when valid; otherwise a failure result.
        /// </returns>
        public async Task<Result<ResolvedScanSessionContext>> ResolveForBusinessAsync(
            string scanSessionToken,
            Guid businessId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(scanSessionToken))
            {
                return Result<ResolvedScanSessionContext>.Fail("ScanSessionToken is required.");
            }

            if (businessId == Guid.Empty)
            {
                return Result<ResolvedScanSessionContext>.Fail("BusinessId is required.");
            }

            var token = await _db.Set<QrCodeToken>()
                .AsQueryable()
                .SingleOrDefaultAsync(t => t.Token == scanSessionToken && !t.IsDeleted, ct)
                .ConfigureAwait(false);

            if (token is null)
            {
                return Result<ResolvedScanSessionContext>.Fail("Scan session token not found.");
            }

            var now = _clock.UtcNow;

            if (token.ExpiresAtUtc <= now)
            {
                return Result<ResolvedScanSessionContext>.Fail("Scan session token has expired.");
            }

            if (token.ConsumedAtUtc is not null)
            {
                return Result<ResolvedScanSessionContext>.Fail("Scan session token has already been consumed.");
            }

            if (token.ConsumedByBusinessId is not null && token.ConsumedByBusinessId != businessId)
            {
                return Result<ResolvedScanSessionContext>.Fail("Scan session token is bound to a different business.");
            }

            var session = await _db.Set<ScanSession>()
                .AsQueryable()
                .SingleOrDefaultAsync(s => s.QrCodeTokenId == token.Id && !s.IsDeleted, ct)
                .ConfigureAwait(false);

            if (session is null)
            {
                return Result<ResolvedScanSessionContext>.Fail("Scan session not found for the provided token.");
            }

            if (session.BusinessId != businessId)
            {
                return Result<ResolvedScanSessionContext>.Fail("Scan session does not belong to this business.");
            }

            if (session.ExpiresAtUtc <= now)
            {
                if (session.Status == LoyaltyScanStatus.Pending)
                {
                    session.Status = LoyaltyScanStatus.Expired;
                    session.Outcome = "Expired";
                    session.FailureReason = "Session expired before use.";
                    await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                }

                return Result<ResolvedScanSessionContext>.Fail("Scan session has expired.");
            }

            if (session.Status != LoyaltyScanStatus.Pending)
            {
                return Result<ResolvedScanSessionContext>.Fail("Scan session is not in a pending state.");
            }

            return Result<ResolvedScanSessionContext>.Ok(new ResolvedScanSessionContext(token, session));
        }

        /// <summary>
        /// Marks the token as consumed, binding it to the specified business context.
        /// </summary>
        /// <param name="token">Token entity to consume.</param>
        /// <param name="businessId">Business that consumed the token.</param>
        /// <param name="businessLocationId">Optional location that consumed the token.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task ConsumeAsync(
            QrCodeToken token,
            Guid businessId,
            Guid? businessLocationId,
            CancellationToken ct = default)
        {
            if (token is null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (businessId == Guid.Empty)
            {
                throw new ArgumentException("BusinessId must not be empty.", nameof(businessId));
            }

            if (token.ConsumedAtUtc is not null)
            {
                return;
            }

            token.ConsumedAtUtc = _clock.UtcNow;
            token.ConsumedByBusinessId = businessId;
            token.ConsumedByBusinessLocationId = businessLocationId;

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Represents a resolved token/session pair that has passed validation.
        /// </summary>
        public sealed class ResolvedScanSessionContext
        {
            public ResolvedScanSessionContext(QrCodeToken token, ScanSession session)
            {
                Token = token ?? throw new ArgumentNullException(nameof(token));
                Session = session ?? throw new ArgumentNullException(nameof(session));
            }

            public QrCodeToken Token { get; }
            public ScanSession Session { get; }
        }
    }
}
