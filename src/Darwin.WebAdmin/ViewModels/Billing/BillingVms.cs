using Darwin.Application.Billing.DTOs;
using Darwin.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Darwin.WebAdmin.ViewModels.Billing
{
    public sealed class PaymentsListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public PaymentQueueFilter? QueueFilter { get; set; }
        public StripeOperationsVm Stripe { get; set; } = new();
        public TaxOperationsVm Tax { get; set; } = new();
        public PaymentOpsSummaryVm Summary { get; set; } = new();
        public BillingWebhookOpsSummaryVm Webhooks { get; set; } = new();
        public List<ProviderPlaybookVm> Playbooks { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<PaymentListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class TaxComplianceListVm
    {
        public TaxOperationsVm Tax { get; set; } = new();
        public TaxComplianceOpsSummaryVm Summary { get; set; } = new();
        public List<ProviderPlaybookVm> Playbooks { get; set; } = new();
        public List<TaxComplianceInvoiceReviewItemVm> InvoiceItems { get; set; } = new();
        public List<TaxComplianceCustomerReviewItemVm> CustomerItems { get; set; } = new();
    }

    public sealed class BillingPlansListVm
    {
        public string Query { get; set; } = string.Empty;
        public BillingPlanQueueFilter QueueFilter { get; set; } = BillingPlanQueueFilter.All;
        public BillingPlanOpsSummaryVm Summary { get; set; } = new();
        public List<ProviderPlaybookVm> Playbooks { get; set; } = new();
        public List<BillingPlanListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class BusinessSubscriptionsListVm
    {
        public string Query { get; set; } = string.Empty;
        public BusinessSubscriptionQueueFilter QueueFilter { get; set; } = BusinessSubscriptionQueueFilter.All;
        public BusinessSubscriptionOpsSummaryVm Summary { get; set; } = new();
        public List<BusinessSubscriptionListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class BillingWebhooksListVm
    {
        public string Query { get; set; } = string.Empty;
        public BillingWebhookDeliveryQueueFilter QueueFilter { get; set; } = BillingWebhookDeliveryQueueFilter.All;
        public BillingWebhookOpsSummaryVm Summary { get; set; } = new();
        public List<BillingWebhookSubscriptionListItemVm> Subscriptions { get; set; } = new();
        public List<BillingWebhookDeliveryListItemVm> Deliveries { get; set; } = new();
        public List<ProviderPlaybookVm> Playbooks { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class RefundsListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public BillingRefundQueueFilter? QueueFilter { get; set; }
        public BillingWebhookOpsSummaryVm Webhooks { get; set; } = new();
        public RefundOpsSummaryVm Summary { get; set; } = new();
        public List<ProviderPlaybookVm> Playbooks { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<BillingRefundListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class StripeOperationsVm
    {
        public bool Enabled { get; set; }
        public bool PublishableKeyConfigured { get; set; }
        public bool SecretKeyConfigured { get; set; }
        public bool WebhookSecretConfigured { get; set; }
        public bool MerchantDisplayNameConfigured { get; set; }
        public string MerchantDisplayName { get; set; } = string.Empty;
    }

    public sealed class TaxOperationsVm
    {
        public bool VatEnabled { get; set; }
        public decimal DefaultVatRatePercent { get; set; }
        public bool PricesIncludeVat { get; set; }
        public bool AllowReverseCharge { get; set; }
        public bool IssuerConfigured { get; set; }
        public string InvoiceIssuerLegalName { get; set; } = string.Empty;
        public string InvoiceIssuerCountry { get; set; } = string.Empty;
        public bool InvoiceIssuerTaxIdConfigured { get; set; }
        public bool ArchiveReadinessComplete { get; set; }
        public string ArchiveReadinessLabel { get; set; } = string.Empty;
        public bool EInvoiceBaselineReady { get; set; }
        public string EInvoiceBaselineLabel { get; set; } = string.Empty;
        public bool StructuredExportBaselineReady { get; set; }
        public string StructuredExportBaselineLabel { get; set; } = string.Empty;
        public string ComplianceScopeNote { get; set; } = string.Empty;
    }

    public sealed class TaxComplianceOpsSummaryVm
    {
        public int BusinessCustomersMissingVatIdCount { get; set; }
        public int BusinessCustomersVatValidationReviewCount { get; set; }
        public int BusinessInvoicesMissingVatIdCount { get; set; }
        public int ReverseChargeCandidateInvoiceCount { get; set; }
        public int DraftInvoiceCount { get; set; }
        public int DueSoonInvoiceCount { get; set; }
        public int OverdueInvoiceCount { get; set; }
    }

    public sealed class TaxComplianceInvoiceReviewItemVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid? CustomerId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public CustomerTaxProfileType? CustomerTaxProfileType { get; set; }
        public string? CustomerVatId { get; set; }
        public Guid? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public Guid? PaymentId { get; set; }
        public InvoiceStatus Status { get; set; }
        public string Currency { get; set; } = string.Empty;
        public long TotalGrossMinor { get; set; }
        public DateTime DueDateUtc { get; set; }
        public bool RequiresVatId { get; set; }
        public bool IsReverseChargeCandidate { get; set; }
        public bool? ReverseChargeApplied { get; set; }
        public DateTime? ReverseChargeReviewedAtUtc { get; set; }
        public bool IsDueSoon { get; set; }
        public bool IsOverdue { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
    }

    public sealed class TaxComplianceCustomerReviewItemVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid? UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? VatId { get; set; }
        public bool RequiresVatId { get; set; }
        public bool RequiresVatValidation { get; set; }
        public CustomerVatValidationStatus VatValidationStatus { get; set; }
        public DateTime? VatValidationCheckedAtUtc { get; set; }
        public string? VatValidationSource { get; set; }
        public string? VatValidationMessage { get; set; }
        public int OpportunityCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
    }

    public sealed class InvoiceReverseChargeDecisionVm
    {
        [Required]
        public Guid Id { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public bool Applies { get; set; }

        [StringLength(512)]
        public string? Note { get; set; }
    }

    public sealed class TaxComplianceCustomerTaxProfileUpdateVm
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public CustomerTaxProfileType TaxProfileType { get; set; } = CustomerTaxProfileType.Business;

        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        public string VatId { get; set; } = string.Empty;
    }

    public sealed class CustomerVatValidationDecisionVm
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public CustomerVatValidationStatus Status { get; set; }

        [StringLength(80)]
        public string? Source { get; set; }

        [StringLength(512)]
        public string? Message { get; set; }
    }

    public sealed class CustomerVatValidationLookupVm
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class PaymentOpsSummaryVm
    {
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
        public int RefundedCount { get; set; }
        public int UnlinkedCount { get; set; }
        public int ProviderLinkedCount { get; set; }
        public int StripeCount { get; set; }
        public int MissingProviderRefCount { get; set; }
        public int FailedStripeCount { get; set; }
        public int NeedsReconciliationCount { get; set; }
        public int DisputeFollowUpCount { get; set; }
    }

    public sealed class BillingWebhookOpsSummaryVm
    {
        public int ActiveSubscriptionCount { get; set; }
        public int PendingDeliveryCount { get; set; }
        public int FailedDeliveryCount { get; set; }
        public int SucceededDeliveryCount { get; set; }
        public int RetryPendingCount { get; set; }
        public int PaymentExceptionCount { get; set; }
        public int DisputeSignalCount { get; set; }
    }

    public sealed class BillingPlanOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
        public int TrialCount { get; set; }
        public int MissingFeaturesCount { get; set; }
        public int InUseCount { get; set; }
    }

    public sealed class BusinessSubscriptionOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int TrialingCount { get; set; }
        public int PastDueCount { get; set; }
        public int CanceledCount { get; set; }
        public int StripeCount { get; set; }
        public int MissingProviderReferenceCount { get; set; }
        public int CancelAtPeriodEndCount { get; set; }
    }

    public sealed class BusinessSubscriptionListItemVm
    {
        public Guid Id { get; set; }
        public Guid? BusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string? BusinessContactEmail { get; set; }
        public Guid BillingPlanId { get; set; }
        public string PlanCode { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? ProviderCustomerId { get; set; }
        public string? ProviderSubscriptionId { get; set; }
        public string? ProviderCheckoutSessionId { get; set; }
        public SubscriptionStatus Status { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public DateTime? CurrentPeriodEndUtc { get; set; }
        public DateTime? TrialEndsAtUtc { get; set; }
        public DateTime? CanceledAtUtc { get; set; }
        public bool CancelAtPeriodEnd { get; set; }
        public long UnitPriceMinor { get; set; }
        public string Currency { get; set; } = string.Empty;
        public bool IsStripe { get; set; }
        public bool MissingProviderReference { get; set; }
        public string ProviderReferenceState { get; set; } = string.Empty;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class BillingPlanListItemVm
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public long PriceMinor { get; set; }
        public string Currency { get; set; } = string.Empty;
        public BillingInterval Interval { get; set; } = BillingInterval.Month;
        public int IntervalCount { get; set; }
        public int? TrialDays { get; set; }
        public bool IsActive { get; set; }
        public bool HasFeatures { get; set; }
        public int ActiveSubscriptionCount { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class BillingWebhookSubscriptionListItemVm
    {
        public Guid Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public sealed class BillingWebhookDeliveryListItemVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid SubscriptionId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int RetryCount { get; set; }
        public int? ResponseCode { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? LastAttemptAtUtc { get; set; }
        public string? IdempotencyKey { get; set; }
        public bool IsActiveSubscription { get; set; }
        public string RetrySafetyState { get; set; } = string.Empty;
        public string FailureDiagnostics { get; set; } = string.Empty;
        public string EscalationHint { get; set; } = string.Empty;
        public string SuggestedOperatorAction { get; set; } = string.Empty;
        public string SuggestedQueueTarget { get; set; } = string.Empty;
    }

    public sealed class RefundOpsSummaryVm
    {
        public int PendingCount { get; set; }
        public int CompletedCount { get; set; }
        public int FailedCount { get; set; }
        public int StripeCount { get; set; }
        public int NeedsSupportCount { get; set; }
    }

    public sealed class ProviderPlaybookVm
    {
        public string Title { get; set; } = string.Empty;
        public string ScopeNote { get; set; } = string.Empty;
        public string OperatorAction { get; set; } = string.Empty;
        public string SettingsDependency { get; set; } = string.Empty;
    }

    public sealed class PaymentListItemVm
    {
        public Guid Id { get; set; }
        public Guid? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public Guid? InvoiceId { get; set; }
        public InvoiceStatus? InvoiceStatus { get; set; }
        public DateTime? InvoiceDueAtUtc { get; set; }
        public long? InvoiceTotalGrossMinor { get; set; }
        public Guid? CustomerId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public Guid? UserId { get; set; }
        public string UserDisplayName { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? ProviderTransactionRef { get; set; }
        public string? ProviderPaymentIntentRef { get; set; }
        public string? ProviderCheckoutSessionRef { get; set; }
        public string? FailureReason { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public long RefundedAmountMinor { get; set; }
        public long NetCapturedAmountMinor { get; set; }
        public DateTime? LastFinancialEventAtUtc { get; set; }
        public int OpenAgeHours { get; set; }
        public string ProviderReferenceState { get; set; } = string.Empty;
        public string DisputeReviewState { get; set; } = string.Empty;
        public bool IsStripe { get; set; }
        public bool NeedsReconciliation { get; set; }
        public bool NeedsDisputeFollowUp { get; set; }
        public bool NeedsSupportAttention { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class BillingRefundListItemVm
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public Guid PaymentId { get; set; }
        public string PaymentProvider { get; set; } = string.Empty;
        public string? PaymentProviderReference { get; set; }
        public string? PaymentProviderPaymentIntentRef { get; set; }
        public string? PaymentProviderCheckoutSessionRef { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string RefundProvider { get; set; } = string.Empty;
        public string? RefundProviderReference { get; set; }
        public string? RefundProviderPaymentReference { get; set; }
        public string? RefundProviderStatus { get; set; }
        public string? FailureReason { get; set; }
        public DateTime? RequestedAtUtc { get; set; }
        public Guid? CustomerId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public RefundStatus Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public DateTime? LastRefundEventAtUtc { get; set; }
        public int OpenAgeHours { get; set; }
        public string ProviderReferenceState { get; set; } = string.Empty;
        public bool IsStripe { get; set; }
        public bool NeedsSupportAttention { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class PaymentEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public DateTime CreatedAtUtc { get; set; }
        public bool IsStripe { get; set; }
        public string? FailureReason { get; set; }
        public DateTime? LastFinancialEventAtUtc { get; set; }
        public int OpenAgeHours { get; set; }
        public string ProviderReferenceState { get; set; } = string.Empty;
        public string DisputeReviewState { get; set; } = string.Empty;
        public bool NeedsReconciliation { get; set; }
        public bool NeedsDisputeFollowUp { get; set; }

        [Required]
        public Guid BusinessId { get; set; }

        [Display(Name = "PaymentOrderId")]
        public Guid? OrderId { get; set; }
        public string? OrderNumber { get; set; }

        [Display(Name = "PaymentInvoiceId")]
        public Guid? InvoiceId { get; set; }
        public InvoiceStatus? InvoiceStatus { get; set; }
        public DateTime? InvoiceDueAtUtc { get; set; }
        public long? InvoiceTotalGrossMinor { get; set; }

        public Guid? CustomerId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public Guid? UserId { get; set; }
        public string UserDisplayName { get; set; } = string.Empty;
        public string? UserEmail { get; set; }

        [Range(0, long.MaxValue)]
        public long AmountMinor { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = string.Empty;

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [Required]
        [StringLength(120)]
        public string Provider { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ProviderTransactionRef { get; set; }

        [Display(Name = "ProviderPaymentIntentRef")]
        [StringLength(200)]
        public string? ProviderPaymentIntentRef { get; set; }

        [Display(Name = "ProviderCheckoutSessionRef")]
        [StringLength(200)]
        public string? ProviderCheckoutSessionRef { get; set; }

        public DateTime? PaidAtUtc { get; set; }
        public long RefundedAmountMinor { get; set; }
        public long NetCapturedAmountMinor { get; set; }
        public List<PaymentRefundHistoryItemVm> Refunds { get; set; } = new();
        public List<PaymentProviderEventItemVm> ProviderEvents { get; set; } = new();
        public List<PaymentSupportPlaybookVm> SupportPlaybooks { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> CustomerOptions { get; set; } = new();
        public List<SelectListItem> UserOptions { get; set; } = new();
    }

    public sealed class BillingPlanEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        [StringLength(128)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        public string Name { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Description { get; set; }

        [Range(0, long.MaxValue)]
        public long PriceMinor { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = string.Empty;

        public BillingInterval Interval { get; set; } = BillingInterval.Month;

        [Range(1, 3650)]
        public int IntervalCount { get; set; } = 1;

        [Range(0, 3650)]
        public int? TrialDays { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        [StringLength(4000)]
        public string FeaturesJson { get; set; } = "{}";

        public int ActiveSubscriptionCount { get; set; }
        public IEnumerable<SelectListItem> IntervalItems { get; set; } = Array.Empty<SelectListItem>();
    }

    public sealed class PaymentRefundHistoryItemVm
    {
        public Guid Id { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public RefundStatus Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }

    public sealed class PaymentProviderEventItemVm
    {
        public string EventType { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }
        public string? IdempotencyKey { get; set; }
        public string CorrelationKind { get; set; } = string.Empty;
        public string CorrelationReference { get; set; } = string.Empty;
    }

    public sealed class PaymentSupportPlaybookVm
    {
        public string Title { get; set; } = string.Empty;
        public string ScopeNote { get; set; } = string.Empty;
        public string OperatorAction { get; set; } = string.Empty;
    }

    public sealed class FinancialAccountsListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public AccountType? QueueFilter { get; set; }
        public FinancialAccountOpsSummaryVm Summary { get; set; } = new();
        public List<ProviderPlaybookVm> Playbooks { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<FinancialAccountListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class FinancialAccountListItemVm
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        public string? Code { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class FinancialAccountOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int AssetCount { get; set; }
        public int RevenueCount { get; set; }
        public int ExpenseCount { get; set; }
        public int MissingCodeCount { get; set; }
    }

    public sealed class FinancialAccountEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public Guid BusinessId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public AccountType Type { get; set; } = AccountType.Asset;

        [StringLength(64)]
        public string? Code { get; set; }

        public List<SelectListItem> BusinessOptions { get; set; } = new();
    }

    public sealed class ExpensesListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public ExpenseOpsSummaryVm Summary { get; set; } = new();
        public List<ProviderPlaybookVm> Playbooks { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<ExpenseListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class ExpenseListItemVm
    {
        public Guid Id { get; set; }
        public Guid? SupplierId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public long AmountMinor { get; set; }
        public DateTime ExpenseDateUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class ExpenseOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int SupplierLinkedCount { get; set; }
        public int RecentCount { get; set; }
        public int HighValueCount { get; set; }
    }

    public sealed class ExpenseEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public Guid BusinessId { get; set; }

        public Guid? SupplierId { get; set; }

        [Required]
        [StringLength(120)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(0, long.MaxValue)]
        public long AmountMinor { get; set; }

        public DateTime ExpenseDateUtc { get; set; }
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> SupplierOptions { get; set; } = new();
    }

    public sealed class JournalEntriesListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public JournalEntryQueueFilter? QueueFilter { get; set; }
        public JournalEntryOpsSummaryVm Summary { get; set; } = new();
        public List<ProviderPlaybookVm> Playbooks { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<JournalEntryListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class JournalEntryOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int RecentCount { get; set; }
        public int MultiLineCount { get; set; }
    }

    public sealed class JournalEntryListItemVm
    {
        public Guid Id { get; set; }
        public DateTime EntryDateUtc { get; set; }
        public string Description { get; set; } = string.Empty;
        public int LineCount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public long TotalDebitMinor { get; set; }
        public long TotalCreditMinor { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class JournalEntryLineVm
    {
        public Guid? Id { get; set; }

        [Required]
        public Guid AccountId { get; set; }

        [Range(0, long.MaxValue)]
        public long DebitMinor { get; set; }

        [Range(0, long.MaxValue)]
        public long CreditMinor { get; set; }

        [StringLength(500)]
        public string? Memo { get; set; }
    }

    public sealed class JournalEntryEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public Guid BusinessId { get; set; }

        public DateTime EntryDateUtc { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public List<JournalEntryLineVm> Lines { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> AccountOptions { get; set; } = new();
    }
}
