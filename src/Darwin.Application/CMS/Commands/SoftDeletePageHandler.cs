using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.CMS;
using Darwin.Application.Abstractions.Services;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.CMS.Commands
{
    /// <summary>
    /// Soft deletes a <see cref="Page"/> to keep content history intact while removing it
    /// from active listings. The operation is idempotent and succeeds silently if the page
    /// does not exist or is already deleted.
    /// </summary>
    public sealed class SoftDeletePageHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        /// <summary>
        /// Initializes a new instance of the handler with the shared DbContext abstraction.
        /// </summary>
        public SoftDeletePageHandler(IAppDbContext db, IStringLocalizer<ValidationResource>? localizer = null, IClock? clock = null)
        {
            _db = db;
            _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
        }

        /// <summary>
        /// Soft-deletes the specified page by Id. If the page is not found, a failure result is returned.
        /// A row version is required so the delete cannot overwrite a newer admin edit.
        /// </summary>
        /// <param name="id">Page identifier.</param>
        /// <param name="rowVersion">
        /// Concurrency token from the UI; the current entity version must match.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Result> HandleAsync(Guid id, byte[]? rowVersion, CancellationToken ct = default)
        {
            var page = await _db.Set<Page>().FirstOrDefaultAsync(p => p.Id == id, ct);
            if (page is null)
                return Result.Fail(_localizer["PageNotFound"]);

            if (rowVersion is null || rowVersion.Length == 0)
                return Result.Fail(_localizer["RowVersionRequired"]);

            var currentVersion = page.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(rowVersion))
                return Result.Fail(_localizer["PageConcurrencyConflict"]);

            page.IsDeleted = true;
            page.ModifiedAtUtc = _clock.UtcNow;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["PageConcurrencyConflict"]);
            }

            return Result.Ok();
        }
    }
}

