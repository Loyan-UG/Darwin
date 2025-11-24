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
    /// Returns reward tiers for a program (paged).
    /// </summary>
    public sealed class GetLoyaltyRewardTiersPageHandler
    {
        private readonly IAppDbContext _db;

        public GetLoyaltyRewardTiersPageHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<(IReadOnlyList<LoyaltyRewardTierListItemDto> Items, int Total)> HandleAsync(
            Guid loyaltyProgramId,
            int page = 1,
            int pageSize = 50,
            CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var query = _db.Set<LoyaltyRewardTier>()
                .AsNoTracking()
                .Where(x => x.LoyaltyProgramId == loyaltyProgramId && !x.IsDeleted);

            int total = await query.CountAsync(ct);

            var items = await query
                .OrderBy(x => x.PointsRequired)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new LoyaltyRewardTierListItemDto
                {
                    Id = x.Id,
                    LoyaltyProgramId = x.LoyaltyProgramId,
                    PointsRequired = x.PointsRequired,
                    RewardType = x.RewardType,
                    RewardValue = x.RewardValue,
                    Description = x.Description,
                    AllowSelfRedemption = x.AllowSelfRedemption,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}
