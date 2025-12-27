using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Meta.DTOs;
using Darwin.Domain.Entities.Settings;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Meta.Queries
{
    /// <summary>
    /// Returns the minimal set of non-sensitive bootstrap values required by mobile apps.
    /// WebApi should treat this handler as the single source of truth and only act as glue.
    /// </summary>
    public sealed class GetAppBootstrapHandler
    {
        private readonly IAppDbContext _db;

        /// <summary>
        /// Creates a new instance of the handler using the application DbContext abstraction.
        /// </summary>
        /// <param name="db">Application DbContext abstraction.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="db"/> is null.</exception>
        public GetAppBootstrapHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
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
                .SingleOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (settings is null)
            {
                return Result<AppBootstrapDto>.Fail("SiteSetting is missing. This is a server misconfiguration.");
            }

            if (!settings.JwtEnabled)
            {
                return Result<AppBootstrapDto>.Fail("JWT is disabled. Mobile authentication cannot proceed.");
            }

            var audience = settings.JwtAudience?.Trim();
            if (string.IsNullOrWhiteSpace(audience))
            {
                return Result<AppBootstrapDto>.Fail("JwtAudience is not configured. This is a server misconfiguration.");
            }

            if (settings.MobileQrTokenRefreshSeconds <= 0)
            {
                return Result<AppBootstrapDto>.Fail("MobileQrTokenRefreshSeconds must be a positive integer.");
            }

            if (settings.MobileMaxOutboxItems <= 0)
            {
                return Result<AppBootstrapDto>.Fail("MobileMaxOutboxItems must be a positive integer.");
            }

            var dto = new AppBootstrapDto(
                JwtAudience: audience,
                QrTokenRefreshSeconds: settings.MobileQrTokenRefreshSeconds,
                MaxOutboxItems: settings.MobileMaxOutboxItems);

            return Result<AppBootstrapDto>.Ok(dto);
        }
    }
}
