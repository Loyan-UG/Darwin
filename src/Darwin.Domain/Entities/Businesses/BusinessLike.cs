using System;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;

namespace Darwin.Domain.Entities.Businesses
{
    /// <summary>
    /// Represents a user's "like" relationship with a business.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a user-controlled, low-risk toggle. Therefore, it should be physically deleted
    /// when a user removes a like (unlike). No soft-delete semantics are intended for this entity.
    /// </para>
    /// <para>
    /// Persistence guidelines:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Enforce uniqueness for (UserId, BusinessId) (unfiltered unique index).</description></item>
    /// <item><description>Delete rows physically on "unlike".</description></item>
    /// </list>
    /// </remarks>
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
    }
}
