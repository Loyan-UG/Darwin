using System;

namespace Darwin.Contracts.Billing;

/// <summary>
/// Requests updating cancel-at-period-end flag for the current business subscription.
/// </summary>
public sealed class SetCancelAtPeriodEndRequest
{
    /// <summary>
    /// Gets or sets current subscription identifier.
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Gets or sets desired cancel-at-period-end value.
    /// </summary>
    public bool CancelAtPeriodEnd { get; set; }

    /// <summary>
    /// Gets or sets optimistic concurrency token.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
