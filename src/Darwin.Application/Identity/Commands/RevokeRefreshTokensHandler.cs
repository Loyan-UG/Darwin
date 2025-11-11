using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Identity.DTOs;
using Darwin.Shared.Results;
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

        public RevokeRefreshTokensHandler(IJwtTokenService jwt) => _jwt = jwt;

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

            return Task.FromResult(Result<int>.Fail("Nothing to revoke."));
        }
    }
}
