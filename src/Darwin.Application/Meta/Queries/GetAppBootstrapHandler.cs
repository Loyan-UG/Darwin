using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Meta.DTOs;
using Darwin.Domain.Entities.Settings;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Meta.Queries
{
    /// <summary>
    /// Returns the minimal set of non-sensitive bootstrap values required by mobile apps.
    /// WebApi should treat this handler as the single source of truth and only act as glue.
    /// </summary>
    public sealed class GetAppBootstrapHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        /// <summary>
        /// Creates a new instance of the handler using the application DbContext abstraction.
        /// </summary>
        /// <param name="db">Application DbContext abstraction.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="db"/> is null.</exception>
        public GetAppBootstrapHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Loads bootstrap values from the singleton <see cref="SiteSetting"/> row.
        /// This use case intentionally returns only non-sensitive configuration values.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="Result{T}"/> containing <see cref="AppBootstrapDto"/> when configuration is valid;
        /// otherwise a failure result describing the misconfiguration.
        /// </returns>
        public async Task<Result<AppBootstrapDto>> HandleAsync(CancellationToken ct = default)
        {
            // Single-row settings contract: DB schema is expected to enforce exactly one record.
            var settings = await _db.Set<SiteSetting>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .SingleOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (settings is null)
            {
                return Result<AppBootstrapDto>.Fail(_localizer["AppBootstrapSiteSettingsMissing"]);
            }

            if (!settings.JwtEnabled)
            {
                return Result<AppBootstrapDto>.Fail(_localizer["AppBootstrapJwtDisabled"]);
            }

            var audience = settings.JwtAudience?.Trim();
            if (string.IsNullOrWhiteSpace(audience))
            {
                return Result<AppBootstrapDto>.Fail(_localizer["AppBootstrapJwtAudienceMissing"]);
            }

            if (settings.MobileQrTokenRefreshSeconds <= 0)
            {
                return Result<AppBootstrapDto>.Fail(_localizer["AppBootstrapQrTokenRefreshInvalid"]);
            }

            if (settings.MobileMaxOutboxItems <= 0)
            {
                return Result<AppBootstrapDto>.Fail(_localizer["AppBootstrapMaxOutboxItemsInvalid"]);
            }

            var dto = new AppBootstrapDto(
                JwtAudience: audience,
                QrTokenRefreshSeconds: settings.MobileQrTokenRefreshSeconds,
                MaxOutboxItems: settings.MobileMaxOutboxItems);

            return Result<AppBootstrapDto>.Ok(dto);
        }
    }
}
