using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application;
using Darwin.Application.Identity.Validators;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Services;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Issues a new TOTP secret (Base32) and returns an otpauth:// URI for QR code provisioning.
    /// Stores/updates the secret in UserTwoFactorSecret (not activated yet).
    /// </summary>
    public sealed class TotpProvisioningHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<TotpProvisionDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public TotpProvisioningHandler(
            IAppDbContext db,
            IStringLocalizer<ValidationResource>? localizer = null)
            : this(db, new TotpProvisionValidator(), localizer)
        {
        }

        public TotpProvisioningHandler(
            IAppDbContext db,
            IValidator<TotpProvisionDto> validator,
            IStringLocalizer<ValidationResource>? localizer = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
        }

        /// <summary>
        /// Generates a new secret and stores it for the user. If an inactive secret exists, it is replaced.
        /// </summary>
        public async Task<Result<TotpProvisionResult>> HandleAsync(TotpProvisionDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted, ct);
            if (user is null) return Result<TotpProvisionResult>.Fail(_localizer["UserNotFound"]);

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
