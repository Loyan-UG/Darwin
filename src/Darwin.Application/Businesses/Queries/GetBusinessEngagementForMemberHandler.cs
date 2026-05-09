using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Returns business engagement summary plus current-member specific state for mobile detail screens.
    /// </summary>
    public sealed class GetBusinessEngagementForMemberHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public GetBusinessEngagementForMemberHandler(IAppDbContext db, ICurrentUserService currentUser, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result<BusinessEngagementSummaryDto>> HandleAsync(Guid businessId, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return Result<BusinessEngagementSummaryDto>.Fail(_localizer["BusinessIdRequired"]);
            }

            var exists = await _db.Set<Business>()
                .AsNoTracking()
                .AnyAsync(b => b.Id == businessId && !b.IsDeleted && b.IsActive && b.OperationalStatus == BusinessOperationalStatus.Approved, ct)
                .ConfigureAwait(false);

            if (!exists)
            {
                return Result<BusinessEngagementSummaryDto>.Fail(_localizer["BusinessNotFound"]);
            }

            var userId = _currentUser.GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Result<BusinessEngagementSummaryDto>.Fail(_localizer["UserNotAuthenticated"]);
            }

            var stats = await _db.Set<BusinessEngagementStats>()
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.BusinessId == businessId && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            var isLiked = await _db.Set<BusinessLike>()
                .AsNoTracking()
                .AnyAsync(x => x.BusinessId == businessId && x.UserId == userId, ct)
                .ConfigureAwait(false);

            var isFavorited = await _db.Set<BusinessFavorite>()
                .AsNoTracking()
                .AnyAsync(x => x.BusinessId == businessId && x.UserId == userId, ct)
                .ConfigureAwait(false);

            var myReview = await _db.Set<BusinessReview>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && x.UserId == userId && !x.IsDeleted)
                .Select(x => new BusinessReviewItemDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    AuthorName = string.Empty,
                    Rating = x.Rating,
                    Comment = x.Comment,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .SingleOrDefaultAsync(ct)
                .ConfigureAwait(false);

            var recentReviews = await (from review in _db.Set<BusinessReview>().AsNoTracking()
                                       join user in _db.Set<User>().AsNoTracking() on review.UserId equals user.Id
                                       where review.BusinessId == businessId &&
                                             !review.IsDeleted &&
                                             !review.IsHidden &&
                                             !user.IsDeleted
                                       orderby review.CreatedAtUtc descending
                                       select new BusinessReviewItemDto
                                       {
                                           Id = review.Id,
                                           UserId = review.UserId,
                                           AuthorName = string.IsNullOrWhiteSpace(user.FirstName)
                                               ? user.Email
                                               : (user.FirstName + " " + (user.LastName ?? string.Empty)).Trim(),
                                           Rating = review.Rating,
                                           Comment = review.Comment,
                                           CreatedAtUtc = review.CreatedAtUtc
                                       })
                .Take(5)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var dto = new BusinessEngagementSummaryDto
            {
                BusinessId = businessId,
                LikeCount = stats?.LikeCount ?? 0,
                FavoriteCount = stats?.FavoriteCount ?? 0,
                RatingCount = stats?.RatingCount ?? 0,
                RatingAverage = stats?.GetAverageRating(),
                IsLikedByMe = isLiked,
                IsFavoritedByMe = isFavorited,
                MyReview = myReview,
                RecentReviews = recentReviews
            };

            return Result<BusinessEngagementSummaryDto>.Ok(dto);
        }
    }
}
