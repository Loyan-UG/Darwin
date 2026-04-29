using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Common;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Catalog.Queries
{
    /// <summary>
    /// Returns paged add-on groups plus lightweight operational summaries for admin queues.
    /// </summary>
    public sealed class GetAddOnGroupsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetAddOnGroupsPageHandler(IAppDbContext db) => _db = db;

        public async Task<(List<AddOnGroupListItemDto> Items, int Total)> HandleAsync(
            int page = 1,
            int pageSize = 20,
            string? q = null,
            AddOnGroupQueueFilter filter = AddOnGroupQueueFilter.All,
            CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

            var baseQuery = BuildListQuery(AddOnGroupSearch.NormalizeQuery(q));

            baseQuery = filter switch
            {
                AddOnGroupQueueFilter.Inactive => baseQuery.Where(x => !x.IsActive),
                AddOnGroupQueueFilter.Global => baseQuery.Where(x => x.IsGlobal),
                AddOnGroupQueueFilter.Unattached => baseQuery.Where(x => x.AttachmentCount == 0),
                AddOnGroupQueueFilter.VariantLinked => baseQuery.Where(x => x.AttachmentCount > 0 && !x.IsGlobal),
                _ => baseQuery
            };

            var total = await baseQuery.CountAsync(ct);
            var items = await baseQuery
                .OrderByDescending(x => x.ModifiedAtUtc)
                .ThenBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }

        private IQueryable<AddOnGroupListItemDto> BuildListQuery(string? q)
        {
            return _db.Set<AddOnGroup>()
                .AsNoTracking()
                .Where(g => !g.IsDeleted && (q == null || EF.Functions.Like(g.Name, q, QueryLikePattern.EscapeCharacter)))
                .Select(g => new AddOnGroupListItemDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Currency = g.Currency,
                    IsActive = g.IsActive,
                    IsGlobal = g.IsGlobal,
                    OptionsCount = g.Options.Count(x => !x.IsDeleted),
                    AttachmentCount =
                        _db.Set<AddOnGroupVariant>().Count(x => x.AddOnGroupId == g.Id && !x.IsDeleted) +
                        _db.Set<AddOnGroupProduct>().Count(x => x.AddOnGroupId == g.Id && !x.IsDeleted) +
                        _db.Set<AddOnGroupCategory>().Count(x => x.AddOnGroupId == g.Id && !x.IsDeleted) +
                        _db.Set<AddOnGroupBrand>().Count(x => x.AddOnGroupId == g.Id && !x.IsDeleted),
                    ModifiedAtUtc = g.ModifiedAtUtc,
                    RowVersion = g.RowVersion
                });
        }
    }

    public sealed class GetAddOnGroupOpsSummaryHandler
    {
        private readonly IAppDbContext _db;

        public GetAddOnGroupOpsSummaryHandler(IAppDbContext db) => _db = db;

        public async Task<AddOnGroupOpsSummaryDto> HandleAsync(string? q = null, CancellationToken ct = default)
        {
            q = AddOnGroupSearch.NormalizeQuery(q);

            var groups = _db.Set<AddOnGroup>()
                .AsNoTracking()
                .Where(g => !g.IsDeleted && (q == null || EF.Functions.Like(g.Name, q, QueryLikePattern.EscapeCharacter)))
                .Select(g => new
                {
                    g.IsActive,
                    g.IsGlobal,
                    AttachmentCount =
                        _db.Set<AddOnGroupVariant>().Count(x => x.AddOnGroupId == g.Id && !x.IsDeleted) +
                        _db.Set<AddOnGroupProduct>().Count(x => x.AddOnGroupId == g.Id && !x.IsDeleted) +
                        _db.Set<AddOnGroupCategory>().Count(x => x.AddOnGroupId == g.Id && !x.IsDeleted) +
                        _db.Set<AddOnGroupBrand>().Count(x => x.AddOnGroupId == g.Id && !x.IsDeleted)
                });

            return new AddOnGroupOpsSummaryDto
            {
                TotalCount = await groups.CountAsync(ct),
                InactiveCount = await groups.CountAsync(x => !x.IsActive, ct),
                GlobalCount = await groups.CountAsync(x => x.IsGlobal, ct),
                UnattachedCount = await groups.CountAsync(x => x.AttachmentCount == 0, ct),
                VariantLinkedCount = await groups.CountAsync(x => x.AttachmentCount > 0 && !x.IsGlobal, ct)
            };
        }
    }

    internal static class AddOnGroupSearch
    {
        public static string? NormalizeQuery(string? q)
        {
            return string.IsNullOrWhiteSpace(q) ? null : QueryLikePattern.Contains(q);
        }
    }
}
