using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.SEO.DTOs;
using Darwin.Domain.Entities.SEO;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.SEO.Queries
{
    /// <summary>
    /// Returns a paged redirect rules list for Admin with basic sorting.
    /// </summary>
    public sealed class GetRedirectRulesPageHandler
    {
        private readonly IAppDbContext _db;
        public GetRedirectRulesPageHandler(IAppDbContext db) => _db = db;

        public async Task<(List<RedirectRuleListItemDto> Items, int Total)> HandleAsync(int page, int pageSize, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery = _db.Set<RedirectRule>().AsNoTracking().Where(r => !r.IsDeleted);
            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery.OrderByDescending(r => r.ModifiedAtUtc ?? r.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(r => new RedirectRuleListItemDto
                {
                    Id = r.Id,
                    FromPath = r.FromPath,
                    To = r.To,
                    IsPermanent = r.IsPermanent,
                    ModifiedAtUtc = r.ModifiedAtUtc
                }).ToListAsync(ct);

            return (items, total);
        }
    }
}
