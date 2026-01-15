using System;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;

namespace Darwin.Domain.Entities.Businesses
{
    /// <summary>
    /// Represents a user's "like" relationship with a business.
    /// Storing likes as rows (instead of a counter) enables:
    /// - "Did I like it?" per-user state
    /// - Accurate like counts with soft-delete history
    /// - Abuse mitigation (rate limiting, constraints, auditing)
    ///
    /// Persistence guidelines:
    /// - Enforce uniqueness for (UserId, BusinessId) where IsDeleted = false.
    /// - Soft delete represents "unlike".
    /// </summary>
    public sealed class BusinessLike : BaseEntity
    {
        /// <summary>
        /// The liked business identifier.
        /// </summary>
        public Guid BusinessId { get; private set; }

        /// <summary>
        /// The user who liked the business.
        /// </summary>
        public Guid UserId { get; private set; }

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
        private BusinessLike() { }

        /// <summary>
        /// Creates a new like relation between a user and a business.
        /// </summary>
        /// <param name="userId">Platform user identifier.</param>
        /// <param name="businessId">Business identifier.</param>
        /// <exception cref="ArgumentException">Thrown when any identifier is empty.</exception>
        public BusinessLike(Guid userId, Guid businessId)
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
        }

        /// <summary>
        /// Soft-deletes the like to represent an "unlike".
        /// The operation is idempotent.
        /// </summary>
        public void Unlike() => IsDeleted = true;

        /// <summary>
        /// Restores the like to represent a "re-like".
        /// The operation is idempotent.
        /// </summary>
        public void Relike() => IsDeleted = false;
    }
}
