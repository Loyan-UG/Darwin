using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Security;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Creates a new (pending) TOTP secret for a user and returns the otpauth URI for QR code.
    /// If there is an existing active secret, it remains; verification will activate the new one and can optionally disable previous ones.
    /// </summary>
    public sealed class BeginTotpProvisioningHandler
    {
        private readonly IAppDbContext _db;
        private readonly ITotpService _totp;
        private readonly IValidator<BeginTotpProvisioningDto> _validator;

        public BeginTotpProvisioningHandler(IAppDbContext db, ITotpService totp, IValidator<BeginTotpProvisioningDto> validator)
        {
            _db = db; _totp = totp; _validator = validator;
        }

        public async Task<Result<TotpProvisioningResultDto>> HandleAsync(BeginTotpProvisioningDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted, ct);
            if (user == null) return Result<TotpProvisioningResultDto>.Fail("User not found.");

            var secret = await _totp.NewSecretBase32Async(ct);
            var pending = new UserTwoFactorSecret(dto.UserId, secret, dto.Label, dto.Issuer);

            _db.Set<UserTwoFactorSecret>().Add(pending);
            await _db.SaveChangesAsync(ct);

            var otpUri = Darwin.Shared.Security.Totp.BuildOtpAuthUri(dto.Issuer, dto.Label, secret);
            return Result<TotpProvisioningResultDto>.Ok(new TotpProvisioningResultDto
            {
                SecretBase32 = secret,
                OtpAuthUri = otpUri
            });
        }
    }
}
