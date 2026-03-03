using System;
using System.Collections.Generic;

namespace Darwin.Contracts.Businesses
{
    /// <summary>
    /// Consumer-facing engagement snapshot for a single business including
    /// current-user state (liked/favorited/my review) and recent public reviews.
    /// </summary>
    public sealed class BusinessEngagementSummaryResponse
    {
        public Guid BusinessId { get; init; }
        public int LikeCount { get; init; }
        public int FavoriteCount { get; init; }
        public int RatingCount { get; init; }
        public decimal? RatingAverage { get; init; }

        public bool IsLikedByMe { get; init; }
        public bool IsFavoritedByMe { get; init; }

        public BusinessReviewItem? MyReview { get; init; }

        public List<BusinessReviewItem> RecentReviews { get; init; } = new();
    }
}