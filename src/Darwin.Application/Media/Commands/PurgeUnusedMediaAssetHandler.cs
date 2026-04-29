using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.CMS;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.CMS.Media.Commands
{
    /// <summary>
    /// Permanently removes a media asset only when no active product references remain.
    /// </summary>
    public sealed class PurgeUnusedMediaAssetHandler
    {
        private const int MaxBulkPurgeSize = 100;

        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public PurgeUnusedMediaAssetHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task<Result> HandleAsync(Guid id, byte[]? rowVersion, CancellationToken ct = default)
        {
            var entity = await _db.Set<MediaAsset>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == id, ct)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return Result.Fail(_localizer["MediaAssetNotFound"]);
            }

            if (rowVersion is null || rowVersion.Length == 0)
            {
                return Result.Fail(_localizer["RowVersionRequired"]);
            }

            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(rowVersion))
            {
                return Result.Fail(_localizer["ConcurrencyConflictDetected"]);
            }

            var isReferenced = await _db.Set<ProductMedia>()
                .AsNoTracking()
                .AnyAsync(pm => !pm.IsDeleted && pm.MediaAssetId == id, ct)
                .ConfigureAwait(false);

            if (isReferenced)
            {
                return Result.Fail(_localizer["MediaAssetStillReferenced"]);
            }

            _db.Set<MediaAsset>().Remove(entity);
            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ConcurrencyConflictDetected"]);
            }

            return Result.Ok();
        }

        public async Task<Result<PurgeUnusedMediaAssetsResultDto>> HandleBatchAsync(int take = MaxBulkPurgeSize, CancellationToken ct = default)
        {
            var normalizedTake = Math.Clamp(take, 1, MaxBulkPurgeSize);
            var candidates = await _db.Set<MediaAsset>()
                .Where(m => !m.IsDeleted && !_db.Set<ProductMedia>().Any(pm => !pm.IsDeleted && pm.MediaAssetId == m.Id))
                .OrderBy(m => m.ModifiedAtUtc ?? m.CreatedAtUtc)
                .Take(normalizedTake)
                .Select(m => new
                {
                    m.Id,
                    m.Url
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (candidates.Count == 0)
            {
                return Result<PurgeUnusedMediaAssetsResultDto>.Ok(new PurgeUnusedMediaAssetsResultDto());
            }

            var candidateIds = candidates.Select(x => x.Id).ToList();
            var entities = await _db.Set<MediaAsset>()
                .Where(m => candidateIds.Contains(m.Id))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            _db.Set<MediaAsset>().RemoveRange(entities);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            return Result<PurgeUnusedMediaAssetsResultDto>.Ok(new PurgeUnusedMediaAssetsResultDto
            {
                PurgedCount = entities.Count,
                PurgedUrls = candidates.Select(x => x.Url).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
            });
        }
    }

    public sealed class PurgeUnusedMediaAssetsResultDto
    {
        public int PurgedCount { get; init; }
        public IReadOnlyList<string> PurgedUrls { get; init; } = Array.Empty<string>();
    }
}
