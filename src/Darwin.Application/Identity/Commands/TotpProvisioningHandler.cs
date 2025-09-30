using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Services;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Issues a new TOTP secret (Base32) and returns an otpauth:// URI for QR code provisioning.
    /// Stores/updates the secret in UserTwoFactorSecret (not activated yet).
    /// </summary>
    public sealed class TotpProvisioningHandler
    {
        private readonly IAppDbContext _db;

        public TotpProvisioningHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Generates a new secret and stores it for the user. If an inactive secret exists, it is replaced.
        /// </summary>
        public async Task<Result<TotpProvisionResult>> HandleAsync(TotpProvisionDto dto, CancellationToken ct = default)
        {
            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted, ct);
            if (user is null) return Result<TotpProvisionResult>.Fail("User not found.");

            var secret = TotpUtility.GenerateSecretBase32();
            var label = string.IsNullOrWhiteSpace(dto.AccountLabelOverride) ? user.Email : dto.AccountLabelOverride!;
            var uri = TotpUtility.BuildOtpAuthUri(dto.Issuer, label, secret);

            // Remove previous inactive secrets
            var stale = await _db.Set<UserTwoFactorSecret>()
                .Where(s => s.UserId == user.Id && s.ActivatedAtUtc == null && !s.IsDeleted)
                .ToListAsync(ct);
            if (stale.Count > 0) _db.Set<UserTwoFactorSecret>().RemoveRange(stale);

            var entity = new UserTwoFactorSecret(user.Id, secret, label, dto.Issuer);
            _db.Set<UserTwoFactorSecret>().Add(entity);

            await _db.SaveChangesAsync(ct);
            return Result<TotpProvisionResult>.Ok(new TotpProvisionResult { SecretBase32 = secret, OtpAuthUri = uri });
        }
    }
}
