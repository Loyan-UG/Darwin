using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Queries
{
    /// <summary>
    /// Returns a paged list of loyalty programs for admin/business UIs.
    /// </summary>
    public sealed class GetLoyaltyProgramsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetLoyaltyProgramsPageHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<(IReadOnlyList<LoyaltyProgramListItemDto> Items, int Total)> HandleAsync(
            int page = 1,
            int pageSize = 20,
            Guid? businessId = null,
            LoyaltyProgramQueueFilter filter = LoyaltyProgramQueueFilter.All,
            CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var query = _db.Set<LoyaltyProgram>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (businessId.HasValue)
                query = query.Where(x => x.BusinessId == businessId.Value);

            query = filter switch
            {
                LoyaltyProgramQueueFilter.Active => query.Where(x => x.IsActive),
                LoyaltyProgramQueueFilter.Inactive => query.Where(x => !x.IsActive),
                LoyaltyProgramQueueFilter.PerCurrencyUnit => query.Where(x => x.AccrualMode == LoyaltyAccrualMode.PerCurrencyUnit),
                LoyaltyProgramQueueFilter.MissingRules => query.Where(x => x.RulesJson == null || x.RulesJson == string.Empty),
                _ => query
            };

            int total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(x => x.ModifiedAtUtc ?? x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new LoyaltyProgramListItemDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    AccrualMode = x.AccrualMode,
                    IsActive = x.IsActive,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task<LoyaltyProgramOpsSummaryDto> GetSummaryAsync(Guid? businessId = null, CancellationToken ct = default)
        {
            var query = _db.Set<LoyaltyProgram>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (businessId.HasValue)
                query = query.Where(x => x.BusinessId == businessId.Value);

            return new LoyaltyProgramOpsSummaryDto
            {
                TotalCount = await query.CountAsync(ct).ConfigureAwait(false),
                ActiveCount = await query.CountAsync(x => x.IsActive, ct).ConfigureAwait(false),
                InactiveCount = await query.CountAsync(x => !x.IsActive, ct).ConfigureAwait(false),
                PerCurrencyUnitCount = await query.CountAsync(x => x.AccrualMode == LoyaltyAccrualMode.PerCurrencyUnit, ct).ConfigureAwait(false),
                MissingRulesCount = await query.CountAsync(x => x.RulesJson == null || x.RulesJson == string.Empty, ct).ConfigureAwait(false)
            };
        }
    }
}
