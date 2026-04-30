using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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
        private readonly IValidator<RefreshRequestDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public RefreshTokenHandler(
            IAppDbContext db,
            IJwtTokenService jwt,
            IValidator<RefreshRequestDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _jwt = jwt;
            _validator = validator;
            _localizer = localizer;
        }

        public async Task<Result<AuthResultDto>> HandleAsync(RefreshRequestDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var userId = await _jwt.ValidateRefreshTokenAsync(dto.RefreshToken, dto.DeviceId, ct).ConfigureAwait(false);
            if (userId is null)
                return Result<AuthResultDto>.Fail(_localizer["InvalidOrExpiredRefreshToken"]);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted, ct);
            if (user is null)
                return Result<AuthResultDto>.Fail(_localizer["UserNotFoundOrInactive"]);

            // Rotation: revoke the used refresh token and issue a new one for the same device.
            // DeviceId is forwarded so that device-bound refresh tokens remain consistent with SiteSetting.
            await _jwt.RevokeRefreshTokenAsync(dto.RefreshToken, dto.DeviceId, ct).ConfigureAwait(false);

            var (access, accessExp, refresh, refreshExp) =
                await _jwt.IssueTokensAsync(user.Id, user.Email, dto.DeviceId, scopes: null, preferredBusinessId: dto.BusinessId, ct).ConfigureAwait(false);


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
