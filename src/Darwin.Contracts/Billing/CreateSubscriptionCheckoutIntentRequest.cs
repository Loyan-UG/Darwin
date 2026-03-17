using System;

namespace Darwin.Contracts.Billing;

/// <summary>
/// Starts a provider checkout/upgrade intent for a target billing plan.
/// </summary>
public sealed class CreateSubscriptionCheckoutIntentRequest
{
    /// <summary>
    /// Gets or sets selected target plan identifier.
    /// </summary>
    public Guid PlanId { get; set; }
}
