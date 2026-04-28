using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Pricing.DTOs;
using Darwin.Domain.Entities.Pricing;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Pricing.Queries
{
    /// <summary>
    /// Returns a paged list of promotions for Admin listing.
    /// </summary>
    public sealed class GetPromotionsPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;
        public GetPromotionsPageHandler(IAppDbContext db) => _db = db ?? throw new System.ArgumentNullException(nameof(db));

        public async Task<(List<PromotionListItemDto> Items, int Total)> HandleAsync(int page, int pageSize, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var baseQuery = _db.Set<Promotion>().AsNoTracking().Where(p => !p.IsDeleted);
            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderByDescending(p => p.ModifiedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(p => new PromotionListItemDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code,
                    IsActive = p.IsActive,
                    StartsAtUtc = p.StartsAtUtc,
                    EndsAtUtc = p.EndsAtUtc,
                    ModifiedAtUtc = p.ModifiedAtUtc
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}
