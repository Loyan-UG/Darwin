using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Starts WebAuthn registration by generating a creation challenge/options and persisting the challenge in UserToken.
    /// </summary>
    public sealed class BeginRegistrationHandler
    {
        private readonly IAppDbContext _db;
        private readonly IWebAuthnService _webauthn;

        public BeginRegistrationHandler(IAppDbContext db, IWebAuthnService webauthn)
        {
            _db = db; _webauthn = webauthn;
        }

        /// <summary>
        /// Generates PublicKeyCredentialCreationOptions (JSON) for a given user and stores the challenge as a temporary UserToken.
        /// </summary>
        public async Task<Result<WebAuthnBeginRegisterResult>> HandleAsync(WebAuthnBeginRegisterDto dto, CancellationToken ct = default)
        {
            var (optionsJson, challenge) = await _webauthn.BeginRegistrationAsync(dto.UserId, dto.UserName, dto.DisplayName, ct);

            var token = new UserToken(
                userId: dto.UserId,
                purpose: "WebAuthn:Register:Challenge",
                value: Convert.ToBase64String(challenge),
                expiresAtUtc: DateTime.UtcNow.AddMinutes(10)
            );

            _db.Set<UserToken>().Add(token);
            await _db.SaveChangesAsync(ct);

            return Result<WebAuthnBeginRegisterResult>.Ok(new WebAuthnBeginRegisterResult
            {
                ChallengeTokenId = token.Id,
                OptionsJson = optionsJson
            });
        }
    }
}
