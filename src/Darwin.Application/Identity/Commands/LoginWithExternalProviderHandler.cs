using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands;

/// <summary>
/// Exchanges a verified external identity for Darwin access and refresh tokens.
/// Provider SDK validation is delegated to <see cref="IExternalIdentityVerifier"/>
/// so Application remains provider-neutral.
/// </summary>
public sealed class LoginWithExternalProviderHandler
{
    private const string DefaultMemberRoleKey = "Members";

    private readonly IAppDbContext _db;
    private readonly IExternalIdentityVerifier _verifier;
    private readonly IJwtTokenService _jwt;
    private readonly IUserPasswordHasher _hasher;
    private readonly ISecurityStampService _stamps;
    private readonly IValidator<ExternalLoginRequestDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public LoginWithExternalProviderHandler(
        IAppDbContext db,
        IExternalIdentityVerifier verifier,
        IJwtTokenService jwt,
        IUserPasswordHasher hasher,
        ISecurityStampService stamps,
        IValidator<ExternalLoginRequestDto> validator,
        IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));
        _jwt = jwt ?? throw new ArgumentNullException(nameof(jwt));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _stamps = stamps ?? throw new ArgumentNullException(nameof(stamps));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task<Result<AuthResultDto>> HandleAsync(ExternalLoginRequestDto dto, CancellationToken ct = default)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

        var verified = await _verifier.VerifyAsync(dto, ct).ConfigureAwait(false);
        if (!verified.Succeeded || verified.Value is null)
        {
            return Result<AuthResultDto>.Fail(_localizer["InvalidExternalLogin"]);
        }

        var identity = verified.Value;
        if (!identity.EmailVerified || string.IsNullOrWhiteSpace(identity.Email))
        {
            return Result<AuthResultDto>.Fail(_localizer["ExternalLoginRequiresVerifiedEmail"]);
        }

        var normalizedProvider = NormalizeProvider(identity.Provider);
        var normalizedEmail = identity.Email.Trim().ToUpperInvariant();

        var existingLogin = await _db.Set<UserLogin>()
            .Include(l => l.User)
            .FirstOrDefaultAsync(l =>
                !l.IsDeleted &&
                l.Provider == normalizedProvider &&
                l.ProviderKey == identity.ProviderKey,
                ct)
            .ConfigureAwait(false);

        var user = existingLogin?.User;
        if (user is null)
        {
            user = await _db.Set<User>()
                .FirstOrDefaultAsync(u => !u.IsDeleted && u.NormalizedEmail == normalizedEmail, ct)
                .ConfigureAwait(false);

            if (user is not null && !user.EmailConfirmed)
            {
                return Result<AuthResultDto>.Fail(_localizer["EmailAddressNotConfirmed"]);
            }

            if (user is null)
            {
                user = CreateExternalUser(identity);
                _db.Set<User>().Add(user);
                await AttachDefaultMemberRoleAsync(user, ct).ConfigureAwait(false);
            }

            _db.Set<UserLogin>().Add(new UserLogin(
                user.Id,
                normalizedProvider,
                identity.ProviderKey,
                identity.DisplayName));

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        if (!user.IsActive || user.IsDeleted)
        {
            return Result<AuthResultDto>.Fail(_localizer["AccountInactive"]);
        }

        var nowUtc = DateTime.UtcNow;
        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > nowUtc)
        {
            return Result<AuthResultDto>.Fail(_localizer["AccountLocked"]);
        }

        var (access, accessExp, refresh, refreshExp) = await _jwt.IssueTokensAsync(
            user.Id,
            user.Email,
            dto.DeviceId,
            scopes: null,
            preferredBusinessId: dto.BusinessId,
            ct)
            .ConfigureAwait(false);

        return Result<AuthResultDto>.Ok(new AuthResultDto
        {
            AccessToken = access,
            AccessTokenExpiresAtUtc = accessExp,
            RefreshToken = refresh,
            RefreshTokenExpiresAtUtc = refreshExp,
            UserId = user.Id,
            Email = user.Email
        });
    }

    private User CreateExternalUser(ExternalIdentityDto identity)
    {
        var unusablePassword = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        return new User(identity.Email, _hasher.Hash(unusablePassword), _stamps.NewStamp())
        {
            EmailConfirmed = true,
            FirstName = identity.FirstName,
            LastName = identity.LastName,
            IsActive = true,
            IsSystem = false,
            Locale = DomainDefaults.DefaultCulture,
            Currency = DomainDefaults.DefaultCurrency,
            Timezone = DomainDefaults.DefaultTimezone
        };
    }

    private async Task AttachDefaultMemberRoleAsync(User user, CancellationToken ct)
    {
        var memberRoleId = await _db.Set<Role>()
            .Where(r => !r.IsDeleted && r.Key == DefaultMemberRoleKey)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (memberRoleId.HasValue)
        {
            _db.Set<UserRole>().Add(new UserRole(user.Id, memberRoleId.Value));
        }
    }

    private static string NormalizeProvider(string provider)
        => string.Equals(provider, "Google", StringComparison.OrdinalIgnoreCase)
            ? "Google"
            : provider.Trim();
}
