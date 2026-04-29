using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.CMS;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.CMS.Media.Commands
{
    /// <summary>
    /// Performs a logical delete (soft delete) on a media asset to keep references intact.
    /// </summary>
    public sealed class SoftDeleteMediaAssetHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public SoftDeleteMediaAssetHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(Guid id, byte[]? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
                return Result.Fail(_localizer["MediaAssetNotFound"]);

            if (rowVersion is null || rowVersion.Length == 0)
                return Result.Fail(_localizer["RowVersionRequired"]);

            var entity = await _db.Set<MediaAsset>()
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, ct)
                .ConfigureAwait(false);

            if (entity is null)
                return Result.Fail(_localizer["MediaAssetNotFound"]);

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
