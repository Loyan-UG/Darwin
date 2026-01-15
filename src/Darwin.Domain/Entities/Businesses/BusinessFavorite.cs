using System;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;

namespace Darwin.Domain.Entities.Businesses
{
    /// <summary>
    /// Represents a user's "favorite" (bookmark) relationship with a business.
    /// This entity powers mobile features such as:
    /// - Favorites list ("My favorites")
    /// - Personalization signals for feed/ranking (future phases)
    ///
    /// Persistence guidelines:
    /// - Enforce uniqueness for (UserId, BusinessId) for rows where IsDeleted = false.
    /// - Use soft delete for "unfavorite" to preserve audit trails and support restore.
    /// </summary>
    public sealed class BusinessFavorite : BaseEntity
    {
        /// <summary>
        /// The favorited business identifier.
        /// </summary>
        public Guid BusinessId { get; private set; }

        /// <summary>
        /// The user who favorited the business.
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
        private BusinessFavorite() { }

        /// <summary>
        /// Creates a new favorite relation between a user and a business.
        /// </summary>
        /// <param name="userId">Platform user identifier.</param>
        /// <param name="businessId">Business identifier.</param>
        /// <exception cref="ArgumentException">Thrown when any identifier is empty.</exception>
        public BusinessFavorite(Guid userId, Guid businessId)
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
        /// Marks this favorite as removed using soft delete.
        /// The operation is idempotent.
        /// </summary>
        public void Remove() => IsDeleted = true;

        /// <summary>
        /// Restores a previously removed favorite by clearing soft delete flag.
        /// The operation is idempotent.
        /// </summary>
        public void Restore() => IsDeleted = false;
    }
}
