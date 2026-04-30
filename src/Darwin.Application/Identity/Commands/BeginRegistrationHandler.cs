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
    /// Begins a WebAuthn registration ceremony by producing creation options (JSON)
    /// and persisting them as a temporary UserToken. Returns the token id to correlate
    /// with the finish step and the options JSON to pass to the browser API.
    /// </summary>
    public sealed class BeginRegistrationHandler
    {
        private const string Purpose = "WebAuthnRegisterOptions";

        private readonly IAppDbContext _db;
        private readonly IWebAuthnService _webauthn;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private readonly IValidator<WebAuthnBeginRegisterDto> _validator;

        public BeginRegistrationHandler(IAppDbContext db, IWebAuthnService webauthn, IClock clock, IStringLocalizer<ValidationResource> localizer, IValidator<WebAuthnBeginRegisterDto> validator)
        {
            _db = db;
            _webauthn = webauthn;
            _clock = clock;
            _localizer = localizer;
            _validator = validator;
        }

        /// <summary>
        /// Creates WebAuthn credential creation options and stores them in a temporary token.
        /// </summary>
        /// <param name="dto">User id plus optional display fields used for the browser prompt.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Result<WebAuthnBeginRegisterResult>> HandleAsync(
            WebAuthnBeginRegisterDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var user = await _db.Set<User>().AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted, ct);
            if (user is null)
                return Result<WebAuthnBeginRegisterResult>.Fail(_localizer["UserNotFound"]);

            var userName = string.IsNullOrWhiteSpace(dto.UserName) ? user.Email : dto.UserName;
            var displayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? user.Email : dto.DisplayName;

            var (optionsJson, _) = await _webauthn.BeginRegistrationAsync(
                dto.UserId, userName, displayName, ct);

            // Remove any previous unused tokens of the same purpose
            var nowUtc = _clock.UtcNow;
            var oldTokens = await _db.Set<UserToken>()
                .Where(t => t.UserId == dto.UserId &&
                            t.Purpose == Purpose &&
                            (t.UsedAtUtc == null || (t.ExpiresAtUtc != null && t.ExpiresAtUtc <= nowUtc)))
                .ToListAsync(ct);
            if (oldTokens.Count > 0)
                _db.Set<UserToken>().RemoveRange(oldTokens);

            // Store the options JSON with a short expiry (10 minutes)
            var token = new UserToken(dto.UserId, Purpose, optionsJson, nowUtc.AddMinutes(10));
            _db.Set<UserToken>().Add(token);
            await _db.SaveChangesAsync(ct);

            var result = new WebAuthnBeginRegisterResult
            {
                ChallengeTokenId = token.Id,
                OptionsJson = optionsJson
            };
            return Result<WebAuthnBeginRegisterResult>.Ok(result);
        }
    }
}
