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
    /// Begins a WebAuthn login by issuing assertion options and storing them in a temporary token.
    /// Returns the token id and options JSON for the browser.
    /// </summary>
    public sealed class BeginLoginHandler
    {
        private const string Purpose = "WebAuthnLoginOptions";

        private readonly IAppDbContext _db;
        private readonly IWebAuthnService _webauthn;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private readonly IValidator<WebAuthnBeginLoginDto> _validator;

        public BeginLoginHandler(IAppDbContext db, IWebAuthnService webauthn, IClock clock, IStringLocalizer<ValidationResource> localizer, IValidator<WebAuthnBeginLoginDto> validator)
        {
            _db = db;
            _webauthn = webauthn;
            _clock = clock;
            _localizer = localizer;
            _validator = validator;
        }

        /// <summary>
        /// Creates assertion options restricted to the user's known credentials (if any).
        /// </summary>
        /// <param name="dto">User id.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Result<WebAuthnBeginLoginResult>> HandleAsync(
            WebAuthnBeginLoginDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var user = await _db.Set<User>().AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted, ct);
            if (user is null)
                return Result<WebAuthnBeginLoginResult>.Fail(_localizer["UserNotFound"]);

            var allowedIds = await _db.Set<UserWebAuthnCredential>()
                .AsNoTracking()
                .Where(c => c.UserId == dto.UserId && !c.IsDeleted)
                .Select(c => c.CredentialId)
                .ToListAsync(ct);

            var (optionsJson, _) = await _webauthn.BeginLoginAsync(dto.UserId, allowedIds, ct);

            // Remove previous unused tokens for this purpose
            var nowUtc = _clock.UtcNow;
            var oldTokens = await _db.Set<UserToken>()
                .Where(t => t.UserId == dto.UserId &&
                            t.Purpose == Purpose &&
                            (t.UsedAtUtc == null || (t.ExpiresAtUtc != null && t.ExpiresAtUtc <= nowUtc)))
                .ToListAsync(ct);
            if (oldTokens.Count > 0)
                _db.Set<UserToken>().RemoveRange(oldTokens);

            var token = new UserToken(dto.UserId, Purpose, optionsJson, nowUtc.AddMinutes(10));
            _db.Set<UserToken>().Add(token);
            await _db.SaveChangesAsync(ct);

            var result = new WebAuthnBeginLoginResult
            {
                ChallengeTokenId = token.Id,
                OptionsJson = optionsJson
            };
            return Result<WebAuthnBeginLoginResult>.Ok(result);
        }
    }
}
