using System;
using System.Collections.Generic;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Billing
{
    /// <summary>
    /// Defines a commercial subscription plan offered by the platform to business tenants.
    /// </summary>
    public sealed class BillingPlan : BaseEntity
    {
        /// <summary>
        /// Gets or sets the stable plan code used in configuration and provider mapping.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name shown in operator and billing UI.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional plain-text description of the plan.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the recurring price in minor units.
        /// </summary>
        public long PriceMinor { get; set; }

        /// <summary>
        /// Gets or sets the ISO 4217 currency code for <see cref="PriceMinor"/>.
        /// </summary>
        public string Currency { get; set; } = DomainDefaults.DefaultCurrency;

        /// <summary>
        /// Gets or sets the subscription interval unit.
        /// </summary>
        public BillingInterval Interval { get; set; } = BillingInterval.Month;

        /// <summary>
        /// Gets or sets the interval multiplier.
        /// </summary>
        public int IntervalCount { get; set; } = 1;

        /// <summary>
        /// Gets or sets the optional trial duration in days.
        /// </summary>
        public int? TrialDays { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the plan is available for subscription.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets extensible feature metadata as JSON.
        /// Avoid placing secrets in this blob.
        /// </summary>
        public string FeaturesJson { get; set; } = "{}";
    }

    /// <summary>
    /// Represents an active subscription for a business tenant.
    /// </summary>
    public sealed class BusinessSubscription : BaseEntity
    {
        /// <summary>
        /// Gets or sets the owning business id.
        /// </summary>
        public Guid? BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the linked billing plan id.
        /// </summary>
        public Guid BillingPlanId { get; set; }

        /// <summary>
        /// Gets or sets the billing provider name.
        /// </summary>
        public string Provider { get; set; } = "Stripe";

        /// <summary>
        /// Gets or sets the provider-side customer id, if applicable.
        /// </summary>
        public string? ProviderCustomerId { get; set; }

        /// <summary>
        /// Gets or sets the provider-side subscription id.
        /// </summary>
        public string? ProviderSubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the provider-side checkout session id that initiated this subscription.
        /// </summary>
        public string? ProviderCheckoutSessionId { get; set; }

        /// <summary>
        /// Gets or sets the subscription lifecycle status.
        /// </summary>
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trialing;

        /// <summary>
        /// Gets or sets the UTC timestamp when the subscription started.
        /// </summary>
        public DateTime StartedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the current period start timestamp in UTC.
        /// </summary>
        public DateTime? CurrentPeriodStartUtc { get; set; }

        /// <summary>
        /// Gets or sets the current period end timestamp in UTC.
        /// </summary>
        public DateTime? CurrentPeriodEndUtc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the subscription will cancel at period end.
        /// </summary>
        public bool CancelAtPeriodEnd { get; set; }

        /// <summary>
        /// Gets or sets the cancellation timestamp in UTC, if cancelled.
        /// </summary>
        public DateTime? CanceledAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the optional trial end timestamp in UTC.
        /// </summary>
        public DateTime? TrialEndsAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the snapshot unit price in minor units.
        /// </summary>
        public long UnitPriceMinor { get; set; }

        /// <summary>
        /// Gets or sets the snapshot ISO 4217 currency code.
        /// </summary>
        public string Currency { get; set; } = DomainDefaults.DefaultCurrency;

        /// <summary>
        /// Gets or sets optional provider reconciliation metadata as JSON.
        /// </summary>
        public string? MetadataJson { get; set; }
    }

    /// <summary>
    /// Represents a provider-synchronized invoice for a subscription.
    /// </summary>
    public sealed class SubscriptionInvoice : BaseEntity
    {
        /// <summary>
        /// Gets or sets the business subscription id.
        /// </summary>
        public Guid BusinessSubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the business id.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the provider name.
        /// </summary>
        public string Provider { get; set; } = "Stripe";

        /// <summary>
        /// Gets or sets the provider-side invoice id.
        /// </summary>
        public string? ProviderInvoiceId { get; set; }

        /// <summary>
        /// Gets or sets the issue timestamp in UTC.
        /// </summary>
        public DateTime IssuedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the optional due timestamp in UTC.
        /// </summary>
        public DateTime? DueAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the invoice status.
        /// </summary>
        public SubscriptionInvoiceStatus Status { get; set; } = SubscriptionInvoiceStatus.Open;

        /// <summary>
        /// Gets or sets the total amount in minor units.
        /// </summary>
        public long TotalMinor { get; set; }

        /// <summary>
        /// Gets or sets the ISO 4217 currency code.
        /// </summary>
        public string Currency { get; set; } = DomainDefaults.DefaultCurrency;

        /// <summary>
        /// Gets or sets the optional hosted invoice URL.
        /// </summary>
        public string? HostedInvoiceUrl { get; set; }

        /// <summary>
        /// Gets or sets the optional PDF URL.
        /// </summary>
        public string? PdfUrl { get; set; }

        /// <summary>
        /// Gets or sets the payment completion timestamp in UTC.
        /// </summary>
        public DateTime? PaidAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the optional provider failure reason.
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// Gets or sets a JSON snapshot of invoice lines for support and exports.
        /// </summary>
        public string LinesJson { get; set; } = "[]";

        /// <summary>
        /// Gets or sets optional provider metadata as JSON.
        /// </summary>
        public string? MetadataJson { get; set; }
    }

    /// <summary>
    /// Represents a payment transaction used by billing, order settlement, and CRM invoice flows.
    /// This aggregate is intentionally broader than gateway-specific payments so the platform can
    /// evolve toward accounting and ERP integrations without parallel payment concepts.
    /// </summary>
    public sealed class Payment : BaseEntity
    {
        /// <summary>
        /// Gets or sets the owning business id.
        /// This can be null for platform-level order payments that are not yet scoped to a tenant business.
        /// </summary>
        public Guid? BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the optional order id associated with the payment.
        /// </summary>
        public Guid? OrderId { get; set; }

        /// <summary>
        /// Gets or sets the optional invoice id associated with the payment.
        /// </summary>
        public Guid? InvoiceId { get; set; }

        /// <summary>
        /// Gets or sets the optional customer id associated with the payment.
        /// </summary>
        public Guid? CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the optional user id associated with the payment.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the amount in minor units.
        /// </summary>
        public long AmountMinor { get; set; }

        /// <summary>
        /// Gets or sets the ISO 4217 currency code.
        /// </summary>
        public string Currency { get; set; } = DomainDefaults.DefaultCurrency;

        /// <summary>
        /// Gets or sets the payment lifecycle status.
        /// </summary>
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        /// <summary>
        /// Gets or sets the payment provider name such as Stripe or PayPal.
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider transaction reference.
        /// Avoid treating this field as a secret; store provider secrets elsewhere.
        /// </summary>
        public string? ProviderTransactionRef { get; set; }

        /// <summary>
        /// Gets or sets the provider-side payment-intent reference when the gateway exposes one.
        /// This is additive to <see cref="ProviderTransactionRef"/> so existing compatibility paths stay intact.
        /// </summary>
        public string? ProviderPaymentIntentRef { get; set; }

        /// <summary>
        /// Gets or sets the provider-side checkout/session reference when the gateway exposes one.
        /// This is additive to <see cref="ProviderTransactionRef"/> so hosted-checkout and webhook correlation can evolve incrementally.
        /// </summary>
        public string? ProviderCheckoutSessionRef { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when funds were actually paid or captured.
        /// </summary>
        public DateTime? PaidAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the optional provider or operator-supplied failure reason.
        /// This is useful for support and reconciliation when a payment is declined or voided.
        /// </summary>
        public string? FailureReason { get; set; }
    }

    /// <summary>
    /// Represents a financial account used for lightweight double-entry bookkeeping.
    /// </summary>
    public sealed class FinancialAccount : BaseEntity
    {
        /// <summary>
        /// Gets or sets the owning business id.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the account name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the account type.
        /// </summary>
        public AccountType Type { get; set; } = AccountType.Asset;

        /// <summary>
        /// Gets or sets the optional business-specific account code.
        /// </summary>
        public string? Code { get; set; }
    }

    /// <summary>
    /// Represents a journal entry that groups balanced debit and credit lines.
    /// </summary>
    public sealed class JournalEntry : BaseEntity
    {
        /// <summary>
        /// Gets or sets the owning business id.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the accounting entry date in UTC.
        /// </summary>
        public DateTime EntryDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the operator-facing description of the entry.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the journal lines.
        /// </summary>
        public List<JournalEntryLine> Lines { get; set; } = new();
    }

    /// <summary>
    /// Represents one debit or credit line inside a journal entry.
    /// </summary>
    public sealed class JournalEntryLine : BaseEntity
    {
        /// <summary>
        /// Gets or sets the journal entry id.
        /// </summary>
        public Guid JournalEntryId { get; set; }

        /// <summary>
        /// Gets or sets the referenced financial account id.
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// Gets or sets the debit amount in minor units.
        /// </summary>
        public long DebitMinor { get; set; }

        /// <summary>
        /// Gets or sets the credit amount in minor units.
        /// </summary>
        public long CreditMinor { get; set; }

        /// <summary>
        /// Gets or sets the optional memo for audit and review.
        /// </summary>
        public string? Memo { get; set; }
    }

    /// <summary>
    /// Represents a business expense for lightweight accounting and cash-flow reporting.
    /// </summary>
    public sealed class Expense : BaseEntity
    {
        /// <summary>
        /// Gets or sets the owning business id.
        /// </summary>
        public Guid BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the optional supplier id associated with the expense.
        /// </summary>
        public Guid? SupplierId { get; set; }

        /// <summary>
        /// Gets or sets the expense category label.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the human-readable description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expense amount in minor units.
        /// </summary>
        public long AmountMinor { get; set; }

        /// <summary>
        /// Gets or sets the expense date in UTC.
        /// </summary>
        public DateTime ExpenseDateUtc { get; set; }
    }
}
