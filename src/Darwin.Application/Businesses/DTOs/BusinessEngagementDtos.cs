using System;
using System.Collections.Generic;

namespace Darwin.Application.Businesses.DTOs
{
    public sealed class BusinessEngagementSummaryDto
    {
        public Guid BusinessId { get; init; }
        public int LikeCount { get; init; }
        public int FavoriteCount { get; init; }
        public int RatingCount { get; init; }
        public decimal? RatingAverage { get; init; }

        public bool IsLikedByMe { get; init; }
        public bool IsFavoritedByMe { get; init; }

        public BusinessReviewItemDto? MyReview { get; init; }
        public List<BusinessReviewItemDto> RecentReviews { get; init; } = new();
    }

    public sealed class BusinessReviewItemDto
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string AuthorName { get; init; } = string.Empty;
        public byte Rating { get; init; }
        public string? Comment { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }

    public sealed class ToggleBusinessReactionDto
    {
        public bool IsActive { get; init; }
        public int TotalCount { get; init; }
    }

    public sealed class UpsertBusinessReviewDto
    {
        public byte Rating { get; init; }
        public string? Comment { get; init; }
    }
}