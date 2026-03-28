using Darwin.Domain.Enums;

namespace Darwin.Application.CRM.DTOs
{
    /// <summary>
    /// Lightweight CRM interaction row used by timeline-style admin views.
    /// </summary>
    public sealed class InteractionListItemDto
    {
        public Guid Id { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? LeadId { get; set; }
        public Guid? OpportunityId { get; set; }
        public InteractionType Type { get; set; }
        public InteractionChannel Channel { get; set; }
        public string? Subject { get; set; }
        public string? Content { get; set; }
        public Guid? UserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Input DTO used to append a new CRM interaction to a customer, lead, or opportunity timeline.
    /// </summary>
    public sealed class InteractionCreateDto
    {
        public Guid? CustomerId { get; set; }
        public Guid? LeadId { get; set; }
        public Guid? OpportunityId { get; set; }
        public InteractionType Type { get; set; } = InteractionType.Email;
        public InteractionChannel Channel { get; set; } = InteractionChannel.Email;
        public string? Subject { get; set; }
        public string? Content { get; set; }
        public Guid? UserId { get; set; }
    }

    /// <summary>
    /// Lightweight CRM consent row for customer privacy and communication preferences.
    /// </summary>
    public sealed class ConsentListItemDto
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public ConsentType Type { get; set; }
        public bool Granted { get; set; }
        public DateTime GrantedAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Input DTO used to record a new consent decision for a customer.
    /// </summary>
    public sealed class ConsentCreateDto
    {
        public Guid CustomerId { get; set; }
        public ConsentType Type { get; set; } = ConsentType.MarketingEmail;
        public bool Granted { get; set; }
        public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAtUtc { get; set; }
    }

    /// <summary>
    /// Lightweight CRM segment definition row.
    /// </summary>
    public sealed class CustomerSegmentListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MemberCount { get; set; }
        public bool HasDescription { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public enum CustomerSegmentQueueFilter
    {
        All = 0,
        Empty = 1,
        InUse = 2,
        MissingDescription = 3
    }

    public sealed class CustomerSegmentOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int EmptyCount { get; set; }
        public int InUseCount { get; set; }
        public int MissingDescriptionCount { get; set; }
    }

    /// <summary>
    /// Input DTO used to create or update a CRM customer segment definition.
    /// </summary>
    public sealed class CustomerSegmentEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /// <summary>
    /// Lightweight segment membership row rendered on customer edit screens.
    /// </summary>
    public sealed class CustomerSegmentMembershipListItemDto
    {
        public Guid MembershipId { get; set; }
        public Guid SegmentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /// <summary>
    /// Input DTO used to assign a customer to a CRM segment.
    /// </summary>
    public sealed class AssignCustomerSegmentDto
    {
        public Guid CustomerId { get; set; }
        public Guid CustomerSegmentId { get; set; }
    }
}
