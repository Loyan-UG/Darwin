using System;

namespace Darwin.Contracts.Billing;

/// <summary>
/// Result payload for cancel-at-period-end updates.
/// </summary>
public sealed class SetCancelAtPeriodEndResponse
{
    /// <summary>
    /// Gets or sets updated subscription identifier.
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Gets or sets updated cancel-at-period-end value.
    /// </summary>
    public bool CancelAtPeriodEnd { get; set; }

    /// <summary>
    /// Gets or sets updated concurrency token.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
