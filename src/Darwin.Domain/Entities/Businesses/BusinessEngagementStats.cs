using System;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Businesses
{
    /// <summary>
    /// Stores lightweight engagement aggregates for a business, intended to support
    /// mobile discovery lists and detail pages with fast reads:
    /// - Rating counts / sums (for average rating)
    /// - Like count
    /// - Favorite count
    ///
    /// Why keep a separate aggregate table?
    /// - Avoids expensive joins/aggregations for every discovery page request.
    /// - Supports caching and incremental updates (e.g., via Worker or domain events).
    ///
    /// Persistence guidelines:
    /// - Enforce uniqueness for BusinessId (one row per business).
    /// - Keep updates atomic; concurrency is already supported through BaseEntity.RowVersion.
    /// </summary>
    public sealed class BusinessEngagementStats : BaseEntity
    {
        /// <summary>
        /// The business this aggregate row belongs to.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Number of visible (non-deleted, non-hidden) reviews counted in this aggregate.
        /// </summary>
        public int RatingCount { get; set; } = 0;

        /// <summary>
        /// Sum of ratings across visible reviews. Average rating is derived as RatingSum / RatingCount.
        /// Stored as int to keep storage small (rating is 1..5).
        /// </summary>
        public int RatingSum { get; set; } = 0;

        /// <summary>
        /// Total number of active likes for this business.
        /// </summary>
        public int LikeCount { get; set; } = 0;

        /// <summary>
        /// Total number of active favorites for this business.
        /// </summary>
        public int FavoriteCount { get; set; } = 0;

        /// <summary>
        /// UTC timestamp when this aggregate row was last recalculated or updated.
        /// Useful for diagnostics and background reconciliation.
        /// </summary>
        public DateTime? LastCalculatedAtUtc { get; set; }

        /// <summary>
        /// Optional EF navigation to the owning business.
        /// This navigation supports 1:1 mapping and convenient graph traversal.
        /// </summary>
        public Business? Business { get; private set; }

        /// <summary>
        /// Returns the average rating computed from RatingSum/RatingCount.
        /// Returns null when RatingCount is 0.
        /// </summary>
        public decimal? GetAverageRating()
        {
            if (RatingCount <= 0)
            {
                return null;
            }

            return (decimal)RatingSum / RatingCount;
        }

        /// <summary>
        /// Updates the aggregate values in a single operation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is designed for background reconciliation jobs or handler-level aggregate updates.
        /// It enforces basic invariants and stamps <see cref="LastCalculatedAtUtc"/>.
        /// </para>
        /// </remarks>
        /// <param name="ratingCount">Visible review count.</param>
        /// <param name="ratingSum">Sum of visible review ratings.</param>
        /// <param name="likeCount">Active like count.</param>
        /// <param name="favoriteCount">Active favorite count.</param>
        /// <param name="utcNow">UTC timestamp for the recalculation event.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when any numeric argument is negative.</exception>
        public void SetSnapshot(int ratingCount, int ratingSum, int likeCount, int favoriteCount, DateTime utcNow)
        {
            if (ratingCount < 0) throw new ArgumentOutOfRangeException(nameof(ratingCount), "Value must be non-negative.");
            if (ratingSum < 0) throw new ArgumentOutOfRangeException(nameof(ratingSum), "Value must be non-negative.");
            if (likeCount < 0) throw new ArgumentOutOfRangeException(nameof(likeCount), "Value must be non-negative.");
            if (favoriteCount < 0) throw new ArgumentOutOfRangeException(nameof(favoriteCount), "Value must be non-negative.");

            RatingCount = ratingCount;
            RatingSum = ratingSum;
            LikeCount = likeCount;
            FavoriteCount = favoriteCount;
            LastCalculatedAtUtc = utcNow;
        }
    }
}
