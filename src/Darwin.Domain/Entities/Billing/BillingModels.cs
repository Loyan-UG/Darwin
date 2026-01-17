using System;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Billing
{
    /// <summary>
    /// Commercial plan definition for business subscriptions (e.g., "Starter", "Pro").
    /// This is a platform-controlled entity (not business-controlled).
    /// </summary>
    public sealed class BillingPlan : BaseEntity
    {
        /// <summary>
        /// Stable unique code used in configuration and provider mapping (e.g., Stripe price/product mapping).
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Display name shown in UI.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional plain-text description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Recurring unit price in minor units (e.g., cents).
        /// </summary>
        public long PriceMinor { get; set; }

        /// <summary>
        /// Currency for <see cref="PriceMinor"/> (ISO 4217), default EUR.
        /// </summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>
        /// Billing interval for recurring charges.
        /// </summary>
        public BillingInterval Interval { get; set; } = BillingInterval.Month;

        /// <summary>
        /// Multiplier for the interval (e.g., 3 months).
        /// </summary>
        public int IntervalCount { get; set; } = 1;

        /// <summary>
        /// Optional trial period length in days.
        /// </summary>
        public int? TrialDays { get; set; }

        /// <summary>
        /// Whether the plan can be subscribed to.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// JSON containing feature flags and limits (e.g., exports allowed, max staff, etc.).
        /// Keep it evolvable without schema changes.
        /// </summary>
        public string FeaturesJson { get; set; } = "{}";
    }

    /// <summary>
    /// Active subscription for a business. Provider integration (e.g., Stripe) identifiers are stored for reconciliation.
    /// </summary>
    public sealed class BusinessSubscription : BaseEntity
    {
        /// <summary>
        /// Owning business.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Current plan reference.
        /// </summary>
        public Guid BillingPlanId { get; set; }

        /// <summary>
        /// Provider name (e.g., "Stripe"). Kept as string to allow multiple future providers.
        /// </summary>
        public string Provider { get; set; } = "Stripe";

        /// <summary>
        /// Provider customer id for the business (if applicable).
        /// </summary>
        public string? ProviderCustomerId { get; set; }

        /// <summary>
        /// Provider subscription id.
        /// </summary>
        public string? ProviderSubscriptionId { get; set; }

        /// <summary>
        /// Subscription lifecycle status.
        /// </summary>
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trialing;

        /// <summary>
        /// When the subscription started (UTC).
        /// </summary>
        public DateTime StartedAtUtc { get; set; }

        /// <summary>
        /// Current billing period start (UTC), if available from provider.
        /// </summary>
        public DateTime? CurrentPeriodStartUtc { get; set; }

        /// <summary>
        /// Current billing period end (UTC), if available from provider.
        /// </summary>
        public DateTime? CurrentPeriodEndUtc { get; set; }

        /// <summary>
        /// If true, subscription will cancel at the end of the current period.
        /// </summary>
        public bool CancelAtPeriodEnd { get; set; }

        /// <summary>
        /// When cancellation occurred (UTC), if cancelled.
        /// </summary>
        public DateTime? CanceledAtUtc { get; set; }

        /// <summary>
        /// When the trial ends (UTC), if applicable.
        /// </summary>
        public DateTime? TrialEndsAtUtc { get; set; }

        /// <summary>
        /// Snapshot price for the current plan at the time the subscription became active.
        /// This protects reporting if plan prices change later.
        /// </summary>
        public long UnitPriceMinor { get; set; }

        /// <summary>
        /// Currency snapshot for <see cref="UnitPriceMinor"/>.
        /// </summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>
        /// Optional metadata for provider sync, proration decisions, or internal notes.
        /// </summary>
        public string? MetadataJson { get; set; }
    }

    /// <summary>
    /// Invoice record synchronized from provider for a business subscription.
    /// Used for history, support, and basic accounting export.
    /// </summary>
    public sealed class SubscriptionInvoice : BaseEntity
    {
        public Guid BusinessSubscriptionId { get; set; }
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Provider name (e.g., "Stripe").
        /// </summary>
        public string Provider { get; set; } = "Stripe";

        /// <summary>
        /// Provider invoice id.
        /// </summary>
        public string? ProviderInvoiceId { get; set; }

        /// <summary>
        /// Invoice issue time (UTC).
        /// </summary>
        public DateTime IssuedAtUtc { get; set; }

        /// <summary>
        /// Optional due date (UTC).
        /// </summary>
        public DateTime? DueAtUtc { get; set; }

        /// <summary>
        /// Invoice status.
        /// </summary>
        public SubscriptionInvoiceStatus Status { get; set; } = SubscriptionInvoiceStatus.Open;

        /// <summary>
        /// Total amount in minor units.
        /// </summary>
        public long TotalMinor { get; set; }

        /// <summary>
        /// Currency for totals.
        /// </summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>
        /// Optional hosted invoice URL for user self-service.
        /// </summary>
        public string? HostedInvoiceUrl { get; set; }

        /// <summary>
        /// Optional PDF URL (provider-generated).
        /// </summary>
        public string? PdfUrl { get; set; }

        /// <summary>
        /// When paid, timestamp (UTC).
        /// </summary>
        public DateTime? PaidAtUtc { get; set; }

        /// <summary>
        /// Optional failure reason from provider if payment failed.
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// Optional invoice line snapshot for exports and UI rendering.
        /// Example: [{"desc":"Pro plan","amountMinor":999,"qty":1}]
        /// </summary>
        public string LinesJson { get; set; } = "[]";

        /// <summary>
        /// Optional metadata to support reconciliation, tax breakdown, etc.
        /// </summary>
        public string? MetadataJson { get; set; }
    }
}
