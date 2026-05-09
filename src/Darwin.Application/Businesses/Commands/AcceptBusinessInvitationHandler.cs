using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Settings.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Commands
{
    /// <summary>
    /// Accepts a business invitation, provisions or links the invited user, ensures
    /// the correct business membership exists, and issues an authenticated token pair.
    /// </summary>
    public sealed class AcceptBusinessInvitationHandler
    {
        private readonly IAppDbContext _db;
        private readonly IUserPasswordHasher _hasher;
        private readonly ISecurityStampService _stamps;
        private readonly IJwtTokenService _jwt;
        private readonly IClock _clock;
        private readonly IValidator<BusinessInvitationAcceptDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public AcceptBusinessInvitationHandler(
            IAppDbContext db,
            IUserPasswordHasher hasher,
            ISecurityStampService stamps,
            IJwtTokenService jwt,
            IClock clock,
            IValidator<BusinessInvitationAcceptDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
            _stamps = stamps ?? throw new ArgumentNullException(nameof(stamps));
            _jwt = jwt ?? throw new ArgumentNullException(nameof(jwt));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result<BusinessInvitationAcceptanceDto>> HandleAsync(BusinessInvitationAcceptDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var utcNow = _clock.UtcNow;
            var invitation = await _db.Set<BusinessInvitation>()
                .FirstOrDefaultAsync(x => x.Token == dto.Token.Trim() && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (invitation is null)
            {
                return Result<BusinessInvitationAcceptanceDto>.Fail(_localizer["InvitationNotFound"]);
            }

            if (invitation.Status == BusinessInvitationStatus.Revoked)
            {
                return Result<BusinessInvitationAcceptanceDto>.Fail(_localizer["InvitationRevoked"]);
            }

            if (invitation.Status == BusinessInvitationStatus.Accepted)
            {
                return Result<BusinessInvitationAcceptanceDto>.Fail(_localizer["InvitationAlreadyAccepted"]);
            }

            if (invitation.ExpiresAtUtc <= utcNow)
            {
                if (invitation.Status == BusinessInvitationStatus.Pending)
                {
                    invitation.MarkExpired();
                    await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                }

                return Result<BusinessInvitationAcceptanceDto>.Fail(_localizer["InvitationExpired"]);
            }

            var business = await _db.Set<Business>()
                .FirstOrDefaultAsync(x => x.Id == invitation.BusinessId && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (business is null || !IsInvitationAcceptanceAllowed(business))
            {
                return Result<BusinessInvitationAcceptanceDto>.Fail(_localizer["InvitedBusinessUnavailable"]);
            }

            var user = await _db.Set<User>()
                .FirstOrDefaultAsync(x => x.NormalizedEmail == invitation.NormalizedEmail && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            var isNewUser = false;
            if (user is null)
            {
                if (string.IsNullOrWhiteSpace(dto.Password) ||
                    string.IsNullOrWhiteSpace(dto.FirstName) ||
                    string.IsNullOrWhiteSpace(dto.LastName))
                {
                    return Result<BusinessInvitationAcceptanceDto>.Fail(_localizer["InvitedUserDetailsRequired"]);
                }

                object? siteSetting = null;
                try
                {
                    siteSetting = await _db.Set<SiteSetting>()
                        .AsNoTracking()
                        .Where(x => !x.IsDeleted)
                        .OrderBy(x => x.Id)
                        .Select(x => new
                        {
                            x.DefaultCulture,
                            x.TimeZone,
                            x.DefaultCurrency
                        })
                        .FirstOrDefaultAsync(ct)
                        .ConfigureAwait(false);
                }
                catch (InvalidOperationException)
                {
                    // Some lightweight test contexts do not include SiteSetting in the model.
                }

                var defaultCulture = string.IsNullOrWhiteSpace((string?)siteSetting?.GetType().GetProperty("DefaultCulture")?.GetValue(siteSetting))
                    ? SiteSettingDto.DefaultCultureDefault
                    : ((string?)siteSetting?.GetType().GetProperty("DefaultCulture")?.GetValue(siteSetting))!.Trim();
                var defaultTimeZone = string.IsNullOrWhiteSpace((string?)siteSetting?.GetType().GetProperty("TimeZone")?.GetValue(siteSetting))
                    ? SiteSettingDto.TimeZoneDefault
                    : ((string?)siteSetting?.GetType().GetProperty("TimeZone")?.GetValue(siteSetting))!.Trim();
                var defaultCurrency = string.IsNullOrWhiteSpace((string?)siteSetting?.GetType().GetProperty("DefaultCurrency")?.GetValue(siteSetting))
                    ? SiteSettingDto.DefaultCurrencyDefault
                    : ((string?)siteSetting?.GetType().GetProperty("DefaultCurrency")?.GetValue(siteSetting))!.Trim();

                user = new User(invitation.Email, _hasher.Hash(dto.Password.Trim()), _stamps.NewStamp())
                {
                    FirstName = dto.FirstName.Trim(),
                    LastName = dto.LastName.Trim(),
                    Locale = defaultCulture,
                    Timezone = defaultTimeZone,
                    Currency = defaultCurrency,
                    IsActive = true,
                    EmailConfirmed = true
                };

                _db.Set<User>().Add(user);
                isNewUser = true;
            }
            else
            {
                if (!user.IsActive)
                {
                    return Result<BusinessInvitationAcceptanceDto>.Fail(_localizer["InvitedAccountDisabled"]);
                }

                if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > utcNow)
                {
                    return Result<BusinessInvitationAcceptanceDto>.Fail(_localizer["InvitedAccountLocked"]);
                }

                user.EmailConfirmed = true;
            }

            var businessRoleId = await _db.Set<Role>()
                .Where(x => x.Key == "business" && !x.IsDeleted)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (!businessRoleId.HasValue)
            {
                return Result<BusinessInvitationAcceptanceDto>.Fail(_localizer["BusinessUserRoleNotConfigured"]);
            }

            var existingRoleLink = await _db.Set<UserRole>()
                .FirstOrDefaultAsync(x => x.UserId == user.Id && x.RoleId == businessRoleId.Value, ct)
                .ConfigureAwait(false);

            if (existingRoleLink is null)
            {
                _db.Set<UserRole>().Add(new UserRole(user.Id, businessRoleId.Value));
            }
            else if (existingRoleLink.IsDeleted)
            {
                existingRoleLink.IsDeleted = false;
            }

            var membership = await _db.Set<BusinessMember>()
                .FirstOrDefaultAsync(x => x.BusinessId == invitation.BusinessId && x.UserId == user.Id, ct)
                .ConfigureAwait(false);

            if (membership is null)
            {
                membership = new BusinessMember
                {
                    BusinessId = invitation.BusinessId,
                    UserId = user.Id,
                    Role = invitation.Role,
                    IsActive = true
                };

                _db.Set<BusinessMember>().Add(membership);
            }
            else
            {
                if (membership.IsDeleted)
                {
                    membership.IsDeleted = false;
                    membership.Role = invitation.Role;
                }

                if (!membership.IsActive)
                {
                    membership.IsActive = true;
                    membership.Role = invitation.Role;
                }
            }

            invitation.MarkAccepted(user.Id, utcNow);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            var (accessToken, accessTokenExpiresAtUtc, refreshToken, refreshTokenExpiresAtUtc) =
                await _jwt.IssueTokensAsync(user.Id, user.Email, dto.DeviceId, scopes: null, preferredBusinessId: invitation.BusinessId, ct).ConfigureAwait(false);

            return Result<BusinessInvitationAcceptanceDto>.Ok(new BusinessInvitationAcceptanceDto
            {
                AccessToken = accessToken,
                AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc,
                UserId = user.Id,
                Email = user.Email,
                BusinessId = invitation.BusinessId,
                BusinessName = business.Name,
                IsNewUser = isNewUser
            });
        }

        private static bool IsInvitationAcceptanceAllowed(Business business)
            => business.OperationalStatus == BusinessOperationalStatus.PendingApproval ||
               (business.OperationalStatus == BusinessOperationalStatus.Approved && business.IsActive);
    }
}
