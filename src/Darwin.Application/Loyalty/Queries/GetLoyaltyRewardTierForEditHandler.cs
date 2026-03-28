using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Loyalty;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Queries
{
    /// <summary>
    /// Loads a loyalty reward tier for edit screens.
    /// </summary>
    public sealed class GetLoyaltyRewardTierForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetLoyaltyRewardTierForEditHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<LoyaltyRewardTierEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Set<LoyaltyRewardTier>()
                .AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(x => new LoyaltyRewardTierEditDto
                {
                    Id = x.Id,
                    LoyaltyProgramId = x.LoyaltyProgramId,
                    PointsRequired = x.PointsRequired,
                    RewardType = x.RewardType,
                    RewardValue = x.RewardValue,
                    Description = x.Description,
                    AllowSelfRedemption = x.AllowSelfRedemption,
                    MetadataJson = x.MetadataJson,
                    RowVersion = x.RowVersion
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }
    }
}
