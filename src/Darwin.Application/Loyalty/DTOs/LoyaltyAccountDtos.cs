using System;
using Darwin.Domain.Enums;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// Read model for a loyalty account used in mobile/consumer screens.
    /// </summary>
    public sealed class LoyaltyAccountDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
        public LoyaltyAccountStatus Status { get; set; }
        public int PointsBalance { get; set; }
        public int LifetimePoints { get; set; }
        public DateTime? LastAccrualAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Request DTO to get or create (if missing) an account for a user in a business context.
    /// </summary>
    public sealed class GetOrCreateLoyaltyAccountDto
    {
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
    }
}
