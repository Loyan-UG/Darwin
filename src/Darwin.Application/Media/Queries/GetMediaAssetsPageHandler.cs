using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CMS.Media.DTOs;
using Darwin.Domain.Entities.CMS;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CMS.Media.Queries
{
    /// <summary>
    /// Returns a paged list of media assets for Admin grid with basic metadata.
    /// </summary>
    public sealed class GetMediaAssetsPageHandler
    {
        private readonly IAppDbContext _db;
        public GetMediaAssetsPageHandler(IAppDbContext db) => _db = db;

        public async Task<(List<MediaAssetListItemDto> Items, int Total)> HandleAsync(int page, int pageSize, string? query = null, MediaAssetQueueFilter filter = MediaAssetQueueFilter.All, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery = _db.Set<MediaAsset>().AsNoTracking().Where(m => !m.IsDeleted);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                baseQuery = baseQuery.Where(m =>
                    m.Url.Contains(term) ||
                    m.Alt.Contains(term) ||
                    m.OriginalFileName.Contains(term) ||
                    (m.Title != null && m.Title.Contains(term)) ||
                    (m.Role != null && m.Role.Contains(term)));
            }

            baseQuery = filter switch
            {
                MediaAssetQueueFilter.MissingAlt => baseQuery.Where(m => string.IsNullOrWhiteSpace(m.Alt)),
                MediaAssetQueueFilter.EditorAssets => baseQuery.Where(m => m.Role != null && m.Role.Contains("EditorAsset")),
                MediaAssetQueueFilter.LibraryAssets => baseQuery.Where(m => m.Role == null || m.Role == string.Empty || m.Role.Contains("LibraryAsset")),
                MediaAssetQueueFilter.MissingTitle => baseQuery.Where(m => string.IsNullOrWhiteSpace(m.Title)),
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
                    RowVersion = m.RowVersion
                }).ToListAsync(ct);

            return (items, total);
        }
    }

    public sealed class GetMediaAssetOpsSummaryHandler
    {
        private readonly IAppDbContext _db;
        public GetMediaAssetOpsSummaryHandler(IAppDbContext db) => _db = db;

        public async Task<MediaAssetOpsSummaryDto> HandleAsync(CancellationToken ct = default)
        {
            var media = _db.Set<MediaAsset>().AsNoTracking().Where(m => !m.IsDeleted);

            return new MediaAssetOpsSummaryDto
            {
                TotalCount = await media.CountAsync(ct).ConfigureAwait(false),
                MissingAltCount = await media.CountAsync(m => string.IsNullOrWhiteSpace(m.Alt), ct).ConfigureAwait(false),
                MissingTitleCount = await media.CountAsync(m => string.IsNullOrWhiteSpace(m.Title), ct).ConfigureAwait(false),
                EditorAssetCount = await media.CountAsync(m => m.Role != null && m.Role.Contains("EditorAsset"), ct).ConfigureAwait(false),
                LibraryAssetCount = await media.CountAsync(m => m.Role == null || m.Role == string.Empty || m.Role.Contains("LibraryAsset"), ct).ConfigureAwait(false)
            };
        }
    }
}
