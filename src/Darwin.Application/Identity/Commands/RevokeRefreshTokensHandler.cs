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

        public Task<Result<int>> HandleAsync(RevokeRefreshRequestDto dto, CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                _jwt.RevokeRefreshToken(dto.RefreshToken!, dto.DeviceId);
                return Task.FromResult(Result<int>.Ok(1));
            }

            if (dto.UserId.HasValue)
            {
                var count = _jwt.RevokeAllForUser(dto.UserId.Value);
                return Task.FromResult(Result<int>.Ok(count));
            }

            return Task.FromResult(Result<int>.Fail(_localizer["NothingToRevoke"]));
        }
    }
}
