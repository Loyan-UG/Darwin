using System;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// DTO for creating scan sessions (business app flow).
    /// </summary>
    public sealed class ScanSessionCreateDto
    {
        public Guid BusinessId { get; set; }
        public Guid? BusinessLocationId { get; set; }

        public Guid BusinessUserId { get; set; }
        public Guid ConsumerUserId { get; set; }

        public Guid QrCodeTokenId { get; set; }
        public Guid? LoyaltyAccountId { get; set; }

        public DateTime StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }

        public string? ClientSessionId { get; set; }
        public string? ResultJson { get; set; }
    }

    /// <summary>
    /// DTO for viewing scan sessions.
    /// </summary>
    public sealed class ScanSessionViewDto
    {
        public Guid Id { get; set; }
        public byte[]? RowVersion { get; set; }

        public Guid BusinessId { get; set; }
        public Guid? BusinessLocationId { get; set; }

        public Guid BusinessUserId { get; set; }
        public Guid ConsumerUserId { get; set; }

        public Guid QrCodeTokenId { get; set; }
        public Guid? LoyaltyAccountId { get; set; }

        public DateTime StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }

        public string? ClientSessionId { get; set; }
        public string? ResultJson { get; set; }
    }
}
