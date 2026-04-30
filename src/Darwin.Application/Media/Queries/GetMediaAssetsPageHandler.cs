using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.CMS.Media;
using Darwin.Application.CMS.Media.DTOs;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.CMS;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CMS.Media.Queries
{
    /// <summary>
    /// Returns a paged list of media assets for Admin grid with basic metadata.
    /// </summary>
    public sealed class GetMediaAssetsPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;
        public GetMediaAssetsPageHandler(IAppDbContext db) => _db = db ?? throw new System.ArgumentNullException(nameof(db));

        public async Task<(List<MediaAssetListItemDto> Items, int Total)> HandleAsync(int page, int pageSize, string? query = null, MediaAssetQueueFilter filter = MediaAssetQueueFilter.All, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var baseQuery = _db.Set<MediaAsset>().AsNoTracking().Where(m => !m.IsDeleted);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = QueryLikePattern.Contains(query);
                baseQuery = baseQuery.Where(m =>
                    EF.Functions.Like(m.Url, term, QueryLikePattern.EscapeCharacter) ||
                    (m.Alt != null && EF.Functions.Like(m.Alt, term, QueryLikePattern.EscapeCharacter)) ||
                    EF.Functions.Like(m.OriginalFileName, term, QueryLikePattern.EscapeCharacter) ||
                    (m.Title != null && EF.Functions.Like(m.Title, term, QueryLikePattern.EscapeCharacter)) ||
                    (m.Role != null && EF.Functions.Like(m.Role, term, QueryLikePattern.EscapeCharacter)));
            }

            baseQuery = filter switch
            {
                MediaAssetQueueFilter.MissingAlt => baseQuery.Where(m => m.Alt == null || m.Alt.Trim() == string.Empty),
                MediaAssetQueueFilter.EditorAssets => baseQuery.Where(m => m.Role == MediaAssetRoleConventions.EditorAssetRole),
                MediaAssetQueueFilter.LibraryAssets => baseQuery.Where(m => m.Role == null || m.Role == string.Empty || m.Role == MediaAssetRoleConventions.LibraryAssetRole),
                MediaAssetQueueFilter.MissingTitle => baseQuery.Where(m => m.Title == null || m.Title.Trim() == string.Empty),
                MediaAssetQueueFilter.UsedInProducts => baseQuery.Where(m => _db.Set<ProductMedia>().Any(pm => !pm.IsDeleted && pm.MediaAssetId == m.Id)),
                MediaAssetQueueFilter.Unused => baseQuery.Where(m => !_db.Set<ProductMedia>().Any(pm => !pm.IsDeleted && pm.MediaAssetId == m.Id)),
                _ => baseQuery
            };

            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderByDescending(m => m.ModifiedAtUtc ?? m.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(m => new MediaAssetListItemDto
                {
                    Id = m.Id,
                    Url = m.Url,
                    Alt = m.Alt,
                    Title = m.Title,
                    OriginalFileName = m.OriginalFileName,
                    SizeBytes = m.SizeBytes,
                    Width = m.Width,
                    Height = m.Height,
                    Role = m.Role,
                    ModifiedAtUtc = m.ModifiedAtUtc,
                    ProductReferenceCount = _db.Set<ProductMedia>().Count(pm => !pm.IsDeleted && pm.MediaAssetId == m.Id),
                    RowVersion = m.RowVersion
                }).ToListAsync(ct);

            return (items, total);
        }
    }

    public sealed class GetMediaAssetOpsSummaryHandler
    {
        private readonly IAppDbContext _db;
        public GetMediaAssetOpsSummaryHandler(IAppDbContext db) => _db = db ?? throw new System.ArgumentNullException(nameof(db));

        public async Task<MediaAssetOpsSummaryDto> HandleAsync(CancellationToken ct = default)
        {
            var media = _db.Set<MediaAsset>().AsNoTracking().Where(m => !m.IsDeleted);
            var productMedia = _db.Set<ProductMedia>().AsNoTracking().Where(pm => !pm.IsDeleted);

            return await media
                .GroupBy(_ => 1)
                .Select(g => new MediaAssetOpsSummaryDto
                {
                    TotalCount = g.Count(),
                    MissingAltCount = g.Count(m => m.Alt == null || m.Alt.Trim() == string.Empty),
                    MissingTitleCount = g.Count(m => m.Title == null || m.Title.Trim() == string.Empty),
                    EditorAssetCount = g.Count(m => m.Role == MediaAssetRoleConventions.EditorAssetRole),
                    LibraryAssetCount = g.Count(m => m.Role == null || m.Role == string.Empty || m.Role == MediaAssetRoleConventions.LibraryAssetRole),
                    ProductReferencedCount = g.Count(m => productMedia.Any(pm => pm.MediaAssetId == m.Id)),
                    UnusedCount = g.Count(m => !productMedia.Any(pm => pm.MediaAssetId == m.Id))
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false) ?? new MediaAssetOpsSummaryDto();
        }
    }
}
