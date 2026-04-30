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
    /// Completes a WebAuthn login by verifying the assertion and updating the credential's signature counter.
    /// </summary>
    public sealed class FinishLoginHandler
    {
        private const string Purpose = "WebAuthnLoginOptions";

        private readonly IAppDbContext _db;
        private readonly IWebAuthnService _webauthn;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private readonly IValidator<WebAuthnFinishLoginDto> _validator;

        public FinishLoginHandler(IAppDbContext db, IWebAuthnService webauthn, IClock clock, IStringLocalizer<ValidationResource> localizer, IValidator<WebAuthnFinishLoginDto> validator)
        {
            _db = db;
            _webauthn = webauthn;
            _clock = clock;
            _localizer = localizer;
            _validator = validator;
        }

        /// <summary>
        /// Verifies the assertion using the original options JSON retrieved by ChallengeTokenId.
        /// </summary>
        /// <param name="dto">Challenge token id and the raw assertion response JSON.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Result> HandleAsync(WebAuthnFinishLoginDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var nowUtc = _clock.UtcNow;
            var token = await _db.Set<UserToken>()
                .FirstOrDefaultAsync(t => t.Id == dto.ChallengeTokenId
                                          && t.Purpose == Purpose
                                          && t.UsedAtUtc == null
                                          && (t.ExpiresAtUtc == null || t.ExpiresAtUtc > nowUtc), ct);

            if (token is null)
                return Result.Fail(_localizer["LoginSessionExpiredOrMissing"]);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == token.UserId && !u.IsDeleted, ct);
            if (user is null)
                return Result.Fail(_localizer["UserNotFoundForThisSession"]);

            var verified = await _webauthn.FinishLoginAsync(dto.ClientResponseJson, token.Value, ct);
            if (!verified.Ok)
                return Result.Fail(verified.Error ?? _localizer["VerificationFailed"]);

            // Update counter and last-used
            var cred = await _db.Set<UserWebAuthnCredential>()
                .FirstOrDefaultAsync(c =>
                    c.UserId == token.UserId &&
                    c.CredentialId == verified.CredentialId &&
                    !c.IsDeleted,
                    ct);

            if (cred is null)
                return Result.Fail(_localizer["CredentialNotFoundAfterVerification"]);

            cred.SignatureCounter = verified.NewSignCount;
            cred.LastUsedAtUtc = nowUtc;

            token.MarkUsed(nowUtc);
            await _db.SaveChangesAsync(ct);

            return Result.Ok();
        }
    }
}
