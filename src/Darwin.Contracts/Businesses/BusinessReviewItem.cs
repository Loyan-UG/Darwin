using System;

namespace Darwin.Contracts.Businesses
{
    /// <summary>
    /// Lightweight public review model used by mobile detail screens.
    /// </summary>
    public sealed class BusinessReviewItem
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string AuthorName { get; init; } = string.Empty;
        public byte Rating { get; init; }
        public string? Comment { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }
}