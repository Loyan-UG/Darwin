using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Commands
{
    internal static class BusinessEngagementStatsHelper
    {
        public static async Task<int> RecalculateAndGetLikeCountAsync(IAppDbContext db, Guid businessId, CancellationToken ct)
        {
            await RecalculateAsync(db, businessId, ct).ConfigureAwait(false);

            return await db.Set<BusinessEngagementStats>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted)
                .Select(x => x.LikeCount)
                .SingleAsync(ct)
                .ConfigureAwait(false);
        }

        public static async Task<int> RecalculateAndGetFavoriteCountAsync(IAppDbContext db, Guid businessId, CancellationToken ct)
        {
            await RecalculateAsync(db, businessId, ct).ConfigureAwait(false);

            return await db.Set<BusinessEngagementStats>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted)
                .Select(x => x.FavoriteCount)
                .SingleAsync(ct)
                .ConfigureAwait(false);
        }

        public static async Task RecalculateAsync(IAppDbContext db, Guid businessId, CancellationToken ct)
        {
            var stats = await db.Set<BusinessEngagementStats>()
                .SingleOrDefaultAsync(x => x.BusinessId == businessId && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (stats is null)
            {
                stats = new BusinessEngagementStats
                {
                    BusinessId = businessId
                };

                db.Set<BusinessEngagementStats>().Add(stats);
            }

            var ratingCount = await db.Set<BusinessReview>()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted && !x.IsHidden)
                .CountAsync(ct)
                .ConfigureAwait(false);

            var ratingSum = await db.Set<BusinessReview>()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted && !x.IsHidden)
                .Select(x => (int?)x.Rating)
                .SumAsync(ct)
                .ConfigureAwait(false) ?? 0;

            var likeCount = await db.Set<BusinessLike>()
                .Where(x => x.BusinessId == businessId)
                .CountAsync(ct)
                .ConfigureAwait(false);

            var favoriteCount = await db.Set<BusinessFavorite>()
                .Where(x => x.BusinessId == businessId)
                .CountAsync(ct)
                .ConfigureAwait(false);

            stats.SetSnapshot(ratingCount, ratingSum, likeCount, favoriteCount, DateTime.UtcNow);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}