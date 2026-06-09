using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Settings;
using Darwin.Shared.Results;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Identity;

/// <summary>
/// Validates Google ID tokens against the OAuth client ids configured in Site Settings.
/// No provider token is persisted or logged.
/// </summary>
public sealed class GoogleExternalIdentityVerifier : IExternalIdentityVerifier
{
    private readonly IAppDbContext _db;
    private readonly IStringLocalizer<Darwin.Application.ValidationResource> _localizer;
    private readonly ILogger<GoogleExternalIdentityVerifier> _logger;

    public GoogleExternalIdentityVerifier(
        IAppDbContext db,
        IStringLocalizer<Darwin.Application.ValidationResource> localizer,
        ILogger<GoogleExternalIdentityVerifier> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ExternalIdentityDto>> VerifyAsync(ExternalLoginRequestDto request, CancellationToken ct = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        if (!string.Equals(request.Provider, "Google", StringComparison.OrdinalIgnoreCase))
        {
            return Result<ExternalIdentityDto>.Fail(_localizer["UnsupportedExternalLoginProvider"]);
        }

        var settings = await _db.Set<SiteSetting>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => !s.IsDeleted, ct)
            .ConfigureAwait(false);

        if (settings is null || !settings.GoogleExternalLoginEnabled)
        {
            return Result<ExternalIdentityDto>.Fail(_localizer["ExternalLoginProviderNotConfigured"]);
        }

        var audiences = BuildAudiences(settings);
        if (audiences.Length == 0)
        {
            return Result<ExternalIdentityDto>.Fail(_localizer["ExternalLoginProviderNotConfigured"]);
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                    request.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = audiences
                    })
                .ConfigureAwait(false);

            if (payload is null || string.IsNullOrWhiteSpace(payload.Subject))
            {
                return Result<ExternalIdentityDto>.Fail(_localizer["InvalidExternalLogin"]);
            }

            return Result<ExternalIdentityDto>.Ok(new ExternalIdentityDto
            {
                Provider = "Google",
                ProviderKey = payload.Subject,
                Email = payload.Email ?? string.Empty,
                EmailVerified = payload.EmailVerified,
                DisplayName = payload.Name,
                FirstName = payload.GivenName,
                LastName = payload.FamilyName
            });
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Google external login token validation failed.");
            return Result<ExternalIdentityDto>.Fail(_localizer["InvalidExternalLogin"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google external login validation failed because the provider could not be reached or parsed.");
            return Result<ExternalIdentityDto>.Fail(_localizer["InvalidExternalLogin"]);
        }
    }

    private static string[] BuildAudiences(SiteSetting settings)
    {
        var values = new List<string?>
        {
            settings.GoogleExternalLoginAndroidClientId,
            settings.GoogleExternalLoginIosClientId,
            settings.GoogleExternalLoginWebClientId
        };

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
