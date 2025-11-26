using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Exchanges a valid refresh token for a new access token.
    /// Optionally device-binds by comparing DeviceId if provided.
    /// Implements refresh token rotation by issuing a new refresh and invalidating the old.
    /// </summary>
    public sealed class RefreshTokenHandler
    {
        private readonly IAppDbContext _db;
        private readonly IJwtTokenService _jwt;

        public RefreshTokenHandler(IAppDbContext db, IJwtTokenService jwt)
        {
            _db = db;
            _jwt = jwt;
        }

        public async Task<Result<AuthResultDto>> HandleAsync(RefreshRequestDto dto, CancellationToken ct = default)
        {
            var userId = _jwt.ValidateRefreshToken(dto.RefreshToken, dto.DeviceId);
            if (userId is null)
                return Result<AuthResultDto>.Fail("Invalid or expired refresh token.");

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted, ct);
            if (user is null)
                return Result<AuthResultDto>.Fail("User not found or inactive.");

            // Rotation: revoke the used refresh token and issue a new one for the same device.
            // DeviceId is forwarded so that device-bound refresh tokens remain consistent with SiteSetting.
            _jwt.RevokeRefreshToken(dto.RefreshToken, dto.DeviceId);

            var (access, accessExp, refresh, refreshExp) =
                _jwt.IssueTokens(user.Id, user.Email, dto.DeviceId, scopes: null);


            var result = new AuthResultDto
            {
                AccessToken = access,
                AccessTokenExpiresAtUtc = accessExp,
                RefreshToken = refresh,
                RefreshTokenExpiresAtUtc = refreshExp,
                UserId = user.Id,
                Email = user.Email
            };
            return Result<AuthResultDto>.Ok(result);
        }
    }
}
