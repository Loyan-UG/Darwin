using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.SEO;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.SEO.Commands
{
    /// <summary>
    /// Performs a soft delete to preserve audit trail.
    /// </summary>
    public sealed class DeleteRedirectRuleHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public DeleteRedirectRuleHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(Guid id, byte[]? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
                return Result.Fail(_localizer["RedirectRuleNotFound"]);

            if (rowVersion is null || rowVersion.Length == 0)
                return Result.Fail(_localizer["RowVersionRequired"]);

            var entity = await _db.Set<RedirectRule>().FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);
            if (entity == null)
                return Result.Fail(_localizer["RedirectRuleNotFound"]);

            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(rowVersion))
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);

            entity.IsDeleted = true;
            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            return Result.Ok();
        }
    }
}
