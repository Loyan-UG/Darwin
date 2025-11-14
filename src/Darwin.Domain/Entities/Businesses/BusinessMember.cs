using Darwin.Domain.Common;
using Darwin.Domain.Enums;
using System;

namespace Darwin.Domain.Entities.Businesses
{
    /// <summary>
    /// Links a platform user to a business with a specific role and status.
    /// Multiple members can be assigned to the same business (Owner/Manager/Staff).
    /// </summary>
    public sealed class BusinessMember : BaseEntity
    {
        /// <summary>
        /// FK to the business this membership belongs to.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// FK to the platform user who is a member of the business workspace.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Role within the business (capabilities enforced at the application layer).
        /// </summary>
        public BusinessMemberRole Role { get; set; } = BusinessMemberRole.Staff;

        /// <summary>
        /// When false, the member cannot operate the business app (temporarily disabled or offboarded).
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
