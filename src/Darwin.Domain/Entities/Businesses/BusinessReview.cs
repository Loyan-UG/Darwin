using System;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;

namespace Darwin.Domain.Entities.Businesses
{
    /// <summary>
    /// Represents a user-generated review for a business, including:
    /// - Star rating (Phase 2 mobile UX)
    /// - Optional plain-text comment
    /// - Minimal moderation flags (hide/unhide) for future admin workflows
    ///
    /// Persistence guidelines:
    /// - Enforce uniqueness for (UserId, BusinessId) where IsDeleted = false
    ///   so each user has at most one active review per business.
    /// - Consider indexing (BusinessId, IsHidden, IsDeleted) for public review lists.
    /// </summary>
    public sealed class BusinessReview : BaseEntity
    {
        /// <summary>
        /// The reviewed business identifier.
        /// </summary>
        public Guid BusinessId { get; private set; }

        /// <summary>
        /// The authoring user identifier.
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// Rating value stored as a small integer (recommended range: 1..5).
        /// </summary>
        public byte Rating { get; private set; }

        /// <summary>
        /// Optional plain-text comment. Rich formatting belongs to CMS, not reviews.
        /// </summary>
        public string? Comment { get; private set; }

        /// <summary>
        /// When true, the review should be hidden from public/mobile reads.
        /// This is a minimal moderation mechanism; full moderation can evolve later.
        /// </summary>
        public bool IsHidden { get; private set; }

        /// <summary>
        /// Optional internal reason for hiding the review (not meant for public display).
        /// </summary>
        public string? HiddenReason { get; private set; }

        /// <summary>
        /// Optional EF navigation to the business.
        /// </summary>
        public Business? Business { get; private set; }

        /// <summary>
        /// Optional EF navigation to the user.
        /// </summary>
        public User? User { get; private set; }

        /// <summary>
        /// EF Core parameterless constructor.
        /// </summary>
        private BusinessReview() { }

        /// <summary>
        /// Creates a new review.
        /// </summary>
        /// <param name="userId">Authoring user identifier.</param>
        /// <param name="businessId">Reviewed business identifier.</param>
        /// <param name="rating">Rating value (1..5).</param>
        /// <param name="comment">Optional plain-text comment.</param>
        public BusinessReview(Guid userId, Guid businessId, byte rating, string? comment)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("UserId must not be empty.", nameof(userId));
            }

            if (businessId == Guid.Empty)
            {
                throw new ArgumentException("BusinessId must not be empty.", nameof(businessId));
            }

            UserId = userId;
            BusinessId = businessId;

            SetRating(rating);
            SetComment(comment);
        }

        /// <summary>
        /// Updates the review content (rating/comment) for "edit my review" scenarios.
        /// </summary>
        /// <param name="rating">New rating value (1..5).</param>
        /// <param name="comment">New optional comment.</param>
        public void Update(byte rating, string? comment)
        {
            SetRating(rating);
            SetComment(comment);
        }

        /// <summary>
        /// Hides the review from public reads.
        /// </summary>
        /// <param name="reason">Optional internal reason, stored as trimmed plain text.</param>
        public void Hide(string? reason)
        {
            IsHidden = true;
            HiddenReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        }

        /// <summary>
        /// Unhides the review and clears the hidden reason.
        /// </summary>
        public void Unhide()
        {
            IsHidden = false;
            HiddenReason = null;
        }

        private void SetRating(byte rating)
        {
            if (rating < 1 || rating > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(rating), rating, "Rating must be between 1 and 5.");
            }

            Rating = rating;
        }

        private void SetComment(string? comment)
        {
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        }
    }
}
