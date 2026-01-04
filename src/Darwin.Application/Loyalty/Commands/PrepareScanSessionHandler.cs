using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Prepares a new scan session for the current consumer user.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler is the ONLY place where a QR payload token is minted.
    /// External layers must never use internal identifiers such as <see cref="ScanSession.Id"/>.
    /// </para>
    /// <para>
    /// The QR payload contains only a short-lived, opaque <see cref="QrCodeToken.Token"/> string.
    /// The token is linked to a <see cref="ScanSession"/> through <see cref="ScanSession.QrCodeTokenId"/>.
    /// </para>
    /// </remarks>
    public sealed class PrepareScanSessionHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUserService;
        private readonly IClock _clock;

        /// <summary>
        /// Default scan session lifetime in minutes.
        /// </summary>
        private const int DefaultSessionLifetimeMinutes = 5;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrepareScanSessionHandler"/> class.
        /// </summary>
        public PrepareScanSessionHandler(
            IAppDbContext db,
            ICurrentUserService currentUserService,
            IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Creates a new scan session for the current user and returns the data
        /// required to render the QR code (opaque token + basic context).
        /// </summary>
        public async Task<Result<ScanSessionPreparedDto>> HandleAsync(
            PrepareScanSessionDto dto,
            CancellationToken ct = default)
        {
            if (dto is null)
            {
                return Result<ScanSessionPreparedDto>.Fail("Request is required.");
            }

            if (dto.BusinessId == Guid.Empty)
            {
                return Result<ScanSessionPreparedDto>.Fail("BusinessId is required.");
            }

            var userId = _currentUserService.GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Result<ScanSessionPreparedDto>.Fail("User is not authenticated.");
            }

            var account = await _db.Set<LoyaltyAccount>()
                .AsQueryable()
                .SingleOrDefaultAsync(a =>
                        a.BusinessId == dto.BusinessId &&
                        a.UserId == userId &&
                        !a.IsDeleted,
                    ct)
                .ConfigureAwait(false);

            if (account is null)
            {
                return Result<ScanSessionPreparedDto>.Fail("Loyalty account not found for the specified business.");
            }

            if (account.Status != LoyaltyAccountStatus.Active)
            {
                return Result<ScanSessionPreparedDto>.Fail("Loyalty account is not active.");
            }

            string? selectedRewardsJson = null;
            IReadOnlyList<Guid> selectedRewardTierIds = Array.Empty<Guid>();

            if (dto.Mode == LoyaltyScanMode.Redemption && dto.SelectedRewardTierIds.Count > 0)
            {
                var payload = await BuildAndValidateSelectedRewardsPayloadAsync(dto.SelectedRewardTierIds, account, ct)
                    .ConfigureAwait(false);

                if (payload is null)
                {
                    return Result<ScanSessionPreparedDto>.Fail("Insufficient points for selected rewards.");
                }

                selectedRewardsJson = payload.Json;
                selectedRewardTierIds = payload.TierIds;
            }

            var now = _clock.UtcNow;
            var expiresAt = now.AddMinutes(DefaultSessionLifetimeMinutes);

            var qrToken = new QrCodeToken
            {
                UserId = userId,
                LoyaltyAccountId = account.Id,
                Token = GenerateOpaqueToken(),
                Purpose = dto.Mode == LoyaltyScanMode.Redemption ? QrTokenPurpose.Redemption : QrTokenPurpose.Accrual,
                IssuedAtUtc = now,
                ExpiresAtUtc = expiresAt,
                IssuedDeviceId = dto.DeviceId
            };

            // ---------------------------------------------------------------------
            // CRITICAL FIX:
            // Ensure the token entity has a non-empty Id BEFORE referencing it from ScanSession.
            //
            // Why:
            // - BaseEntity.Id does not default to Guid.NewGuid().
            // - EF Core may generate Id only at SaveChanges (ValueGeneratedOnAdd).
            // - ScanSession.QrCodeTokenId must be a valid FK to QrCodeTokens.Id.
            //
            // By assigning a Guid explicitly here, we keep a single SaveChanges call (atomic)
            // and guarantee correct FK correlation.
            // ---------------------------------------------------------------------
            if (qrToken.Id == Guid.Empty)
            {
                qrToken.Id = Guid.NewGuid();
            }

            _db.Set<QrCodeToken>().Add(qrToken);

            var session = new ScanSession
            {
                QrCodeTokenId = qrToken.Id,
                LoyaltyAccountId = account.Id,
                BusinessId = dto.BusinessId,
                BusinessLocationId = dto.BusinessLocationId,
                Mode = dto.Mode,
                Status = LoyaltyScanStatus.Pending,
                SelectedRewardsJson = selectedRewardsJson,
                ExpiresAtUtc = expiresAt,
                CreatedByDeviceId = dto.DeviceId,
                Outcome = "Pending"
            };

            _db.Set<ScanSession>().Add(session);

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            var resultDto = new ScanSessionPreparedDto
            {
                ScanSessionToken = qrToken.Token,
                Mode = session.Mode,
                ExpiresAtUtc = session.ExpiresAtUtc,
                CurrentPointsBalance = account.PointsBalance,
                SelectedRewardTierIds = selectedRewardTierIds
            };

            return Result<ScanSessionPreparedDto>.Ok(resultDto);
        }

        /// <summary>
        /// Validates the selected reward tiers against the loyalty account and returns a payload
        /// describing the selections if valid.
        /// </summary>
        private async Task<SelectedRewardsPayload?> BuildAndValidateSelectedRewardsPayloadAsync(
            IReadOnlyCollection<Guid> rewardTierIds,
            LoyaltyAccount account,
            CancellationToken ct)
        {
            if (rewardTierIds is null || rewardTierIds.Count == 0)
            {
                return null;
            }

            var distinctIds = rewardTierIds.Where(x => x != Guid.Empty).Distinct().ToArray();
            if (distinctIds.Length == 0)
            {
                return null;
            }

            var tiers = await _db.Set<LoyaltyRewardTier>()
                .AsQueryable()
                .Where(t => distinctIds.Contains(t.Id) && !t.IsDeleted)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (tiers.Count == 0)
            {
                return null;
            }

            var items = new List<SelectedRewardItemDto>(tiers.Count);
            var totalRequiredPoints = 0;

            foreach (var tier in tiers)
            {
                var requiredPerUnit = tier.PointsRequired;
                if (requiredPerUnit <= 0)
                {
                    continue;
                }

                items.Add(new SelectedRewardItemDto
                {
                    LoyaltyRewardTierId = tier.Id,
                    Quantity = 1,
                    RequiredPointsPerUnit = requiredPerUnit
                });

                checked
                {
                    totalRequiredPoints += requiredPerUnit;
                }
            }

            if (items.Count == 0 || totalRequiredPoints <= 0)
            {
                return null;
            }

            if (account.PointsBalance < totalRequiredPoints)
            {
                return null;
            }

            var json = JsonSerializer.Serialize(items);
            var acceptedTierIds = items.Select(x => x.LoyaltyRewardTierId).Distinct().ToArray();

            return new SelectedRewardsPayload(json, acceptedTierIds);
        }

        /// <summary>
        /// Creates a random opaque token suitable for QR payload usage.
        /// </summary>
        /// <remarks>
        /// Uses a cryptographically secure random source and encodes as Base64Url without padding.
        /// </remarks>
        private static string GenerateOpaqueToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Base64UrlEncode(bytes);
        }

        /// <summary>
        /// Encodes bytes using Base64Url (RFC 4648) without padding.
        /// </summary>
        private static string Base64UrlEncode(byte[] bytes)
        {
            var base64 = Convert.ToBase64String(bytes);
            return base64
                .Replace("+", "-", StringComparison.Ordinal)
                .Replace("/", "_", StringComparison.Ordinal)
                .TrimEnd('=');
        }

        /// <summary>
        /// Internal payload used by the handler to return both JSON snapshot and accepted tier identifiers.
        /// </summary>
        private sealed class SelectedRewardsPayload
        {
            public SelectedRewardsPayload(string json, IReadOnlyList<Guid> tierIds)
            {
                Json = json ?? throw new ArgumentNullException(nameof(json));
                TierIds = tierIds ?? throw new ArgumentNullException(nameof(tierIds));
            }

            public string Json { get; }
            public IReadOnlyList<Guid> TierIds { get; }
        }
    }
}
