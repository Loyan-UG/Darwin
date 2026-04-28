using Darwin.Application.Abstractions.Auth;
using Darwin.Application;
using Darwin.Application.Identity.DTOs;
using Darwin.Shared.Results;
using Microsoft.Extensions.Localization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Revokes refresh tokens either by explicit token (logout single device) or for a given user (invalidate all devices).
    /// Intended for Admin actions or security events (password reset).
    /// </summary>
    public sealed class RevokeRefreshTokensHandler
    {
        private readonly IJwtTokenService _jwt;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public RevokeRefreshTokensHandler(IJwtTokenService jwt, IStringLocalizer<ValidationResource> localizer)
        {
            _jwt = jwt ?? throw new ArgumentNullException(nameof(jwt));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result<int>> HandleAsync(RevokeRefreshRequestDto dto, CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                await _jwt.RevokeRefreshTokenAsync(dto.RefreshToken!, dto.DeviceId, ct).ConfigureAwait(false);
                return Result<int>.Ok(1);
            }

            if (dto.UserId.HasValue)
            {
                var count = await _jwt.RevokeAllForUserAsync(dto.UserId.Value, ct).ConfigureAwait(false);
                return Result<int>.Ok(count);
            }

            return Result<int>.Fail(_localizer["NothingToRevoke"]);
        }
    }
}
