using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Verifies the WebAuthn attestation using the original options JSON retrieved
    /// via ChallengeTokenId. On success, persists/updates the user's credential.
    /// </summary>
    public sealed class FinishRegistrationHandler
    {
        private const string Purpose = "WebAuthnRegisterOptions";

        private readonly IAppDbContext _db;
        private readonly IWebAuthnService _webauthn;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private readonly IValidator<WebAuthnFinishRegisterDto> _validator;

        public FinishRegistrationHandler(IAppDbContext db, IWebAuthnService webauthn, IClock clock, IStringLocalizer<ValidationResource> localizer, IValidator<WebAuthnFinishRegisterDto> validator)
        {
            _db = db;
            _webauthn = webauthn;
            _clock = clock;
            _localizer = localizer;
            _validator = validator;
        }

        /// <summary>
        /// Completes the registration by validating attestation and saving the credential.
        /// </summary>
        /// <param name="dto">Challenge token id and raw client response JSON (attestation).</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Result<Guid>> HandleAsync(WebAuthnFinishRegisterDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var nowUtc = _clock.UtcNow;
            var token = await _db.Set<UserToken>()
                .FirstOrDefaultAsync(t => t.Id == dto.ChallengeTokenId
                                          && t.Purpose == Purpose
                                          && t.UsedAtUtc == null
                                          && (t.ExpiresAtUtc == null || t.ExpiresAtUtc > nowUtc), ct);

            if (token is null)
                return Result<Guid>.Fail(_localizer["RegistrationSessionExpiredOrMissing"]);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == token.UserId && !u.IsDeleted, ct);
            if (user is null)
                return Result<Guid>.Fail(_localizer["UserNotFoundForThisSession"]);

            var verified = await _webauthn.FinishRegistrationAsync(dto.ClientResponseJson, token.Value, ct);
            if (!verified.Ok)
                return Result<Guid>.Fail(verified.Error ?? _localizer["VerificationFailed"]);

            var existing = await _db.Set<UserWebAuthnCredential>()
                .FirstOrDefaultAsync(c =>
                    c.UserId == user.Id &&
                    c.CredentialId == verified.CredentialId &&
                    !c.IsDeleted,
                    ct);

            if (existing is null)
            {
                var cred = new UserWebAuthnCredential
                {
                    UserId = user.Id,
                    CredentialId = verified.CredentialId,
                    PublicKey = verified.PublicKey,
                    AaGuid = verified.Aaguid,
                    CredentialType = verified.CredType,
                    AttestationFormat = verified.AttestationFmt,
                    SignatureCounter = verified.SignCount,
                    LastUsedAtUtc = nowUtc,
                    IsSyncedPasskey = verified.IsSynced
                };
                _db.Set<UserWebAuthnCredential>().Add(cred);
            }
            else
            {
                existing.PublicKey = verified.PublicKey;
                existing.AaGuid = verified.Aaguid;
                existing.SignatureCounter = verified.SignCount;
                existing.LastUsedAtUtc = nowUtc;
            }

            token.MarkUsed(nowUtc);
            await _db.SaveChangesAsync(ct);

            return Result<Guid>.Ok(user.Id);
        }
    }
}
