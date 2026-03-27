using System;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Businesses
{
    /// <summary>
    /// Explicit audit record for controlled overrides of the "last active owner must remain" business rule.
    /// This keeps exceptional owner-removal actions visible and reviewable without depending on implicit log parsing.
    /// </summary>
    public sealed class BusinessOwnerOverrideAudit : BaseEntity
    {
        /// <summary>
        /// Business whose ownership protection was overridden.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Membership record that was changed or removed under override.
        /// </summary>
        public Guid BusinessMemberId { get; set; }

        /// <summary>
        /// User account affected by the override.
        /// </summary>
        public Guid AffectedUserId { get; set; }

        /// <summary>
        /// Override action kind.
        /// </summary>
        public BusinessOwnerOverrideActionKind ActionKind { get; set; }

        /// <summary>
        /// Operator-entered business reason for allowing the exceptional change.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Optional actor display name captured from the web surface to simplify admin review.
        /// </summary>
        public string? ActorDisplayName { get; set; }
    }
}
