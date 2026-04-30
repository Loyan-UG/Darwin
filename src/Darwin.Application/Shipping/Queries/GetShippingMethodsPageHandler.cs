using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.Shipping;
using Darwin.Application.Shipping.DTOs;
using Darwin.Domain.Entities.Shipping;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Shipping.Queries
{
    /// <summary>
    /// Returns a paged list of shipping methods for the Admin grid.
    /// </summary>
    public sealed class GetShippingMethodsPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;
        public GetShippingMethodsPageHandler(IAppDbContext db) => _db = db ?? throw new System.ArgumentNullException(nameof(db));

        public async Task<(List<ShippingMethodListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string? query = null,
            ShippingMethodQueueFilter filter = ShippingMethodQueueFilter.All,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var baseQuery = _db.Set<ShippingMethod>().AsNoTracking().Where(m => !m.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = QueryLikePattern.Contains(query);
                baseQuery = baseQuery.Where(m =>
                    EF.Functions.Like(m.Name, term, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(m.Carrier, term, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(m.Service, term, QueryLikePattern.EscapeCharacter) ||
                    (m.CountriesCsv != null && EF.Functions.Like(m.CountriesCsv, term, QueryLikePattern.EscapeCharacter)));
            }

            baseQuery = filter switch
            {
                ShippingMethodQueueFilter.Active => baseQuery.Where(m => m.IsActive),
                ShippingMethodQueueFilter.Inactive => baseQuery.Where(m => !m.IsActive),
                ShippingMethodQueueFilter.MissingRates => baseQuery.Where(m => !m.Rates.Any(r => !r.IsDeleted)),
                ShippingMethodQueueFilter.Dhl => baseQuery.Where(m => m.Carrier == ShippingMethodConventions.DhlCarrier),
                ShippingMethodQueueFilter.GlobalCoverage => baseQuery.Where(m => m.CountriesCsv == null || m.CountriesCsv.Trim() == string.Empty),
                ShippingMethodQueueFilter.MultiRate => baseQuery.Where(m => m.Rates.Count(r => !r.IsDeleted) > 1),
                _ => baseQuery
            };

            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .Include(m => m.Rates)
                .OrderBy(m => m.Carrier).ThenBy(m => m.Service)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(m => new ShippingMethodListItemDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Carrier = m.Carrier,
                    Service = m.Service,
                    DisplayName = m.Carrier + " – " + m.Service,
                    CountriesCsv = m.CountriesCsv,
                    Currency = m.Currency,
                    IsActive = m.IsActive,
                    RatesCount = m.Rates.Count(r => !r.IsDeleted),
                    IsDhl = m.Carrier == ShippingMethodConventions.DhlCarrier,
                    HasGlobalCoverage = m.CountriesCsv == null || m.CountriesCsv.Trim() == string.Empty,
                    HasMultipleRates = m.Rates.Count(r => !r.IsDeleted) > 1,
                    ModifiedAtUtc = m.ModifiedAtUtc
                }).ToListAsync(ct);

            return (items, total);
        }
    }

    public sealed class GetShippingMethodOpsSummaryHandler
    {
        private readonly IAppDbContext _db;

        public GetShippingMethodOpsSummaryHandler(IAppDbContext db) => _db = db ?? throw new System.ArgumentNullException(nameof(db));

        public async Task<ShippingMethodOpsSummaryDto> HandleAsync(CancellationToken ct = default)
        {
            var methods = _db.Set<ShippingMethod>().AsNoTracking().Where(m => !m.IsDeleted);

            return await methods
                .GroupBy(_ => 1)
                .Select(g => new ShippingMethodOpsSummaryDto
                {
                    TotalCount = g.Count(),
                    ActiveCount = g.Count(x => x.IsActive),
                    InactiveCount = g.Count(x => !x.IsActive),
                    MissingRatesCount = g.Count(x => !x.Rates.Any(r => !r.IsDeleted)),
                    DhlCount = g.Count(x => x.Carrier == ShippingMethodConventions.DhlCarrier),
                    GlobalCoverageCount = g.Count(x => x.CountriesCsv == null || x.CountriesCsv.Trim() == string.Empty),
                    MultiRateCount = g.Count(x => x.Rates.Count(r => !r.IsDeleted) > 1)
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false) ?? new ShippingMethodOpsSummaryDto();
        }
    }
}
