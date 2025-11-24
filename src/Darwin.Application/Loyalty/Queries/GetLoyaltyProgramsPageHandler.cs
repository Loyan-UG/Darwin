using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Loyalty;
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
            CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var query = _db.Set<LoyaltyProgram>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (businessId.HasValue)
                query = query.Where(x => x.BusinessId == businessId.Value);

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
    }
}
