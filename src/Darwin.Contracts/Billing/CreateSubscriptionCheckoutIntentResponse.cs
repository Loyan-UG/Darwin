using System;

namespace Darwin.Contracts.Billing;

/// <summary>
/// Result payload for subscription checkout intent creation.
/// </summary>
public sealed class CreateSubscriptionCheckoutIntentResponse
{
    /// <summary>
    /// Gets or sets generated checkout URL.
    /// </summary>
    public string CheckoutUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets checkout expiry timestamp (UTC).
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets provider name.
    /// </summary>
    public string Provider { get; set; } = "Stripe";

    /// <summary>
    /// Gets or sets the provider checkout session reference.
    /// </summary>
    public string? ProviderCheckoutSessionReference { get; set; }

    /// <summary>
    /// Gets or sets the provider subscription reference when it is available immediately.
    /// </summary>
    public string? ProviderSubscriptionReference { get; set; }
}
