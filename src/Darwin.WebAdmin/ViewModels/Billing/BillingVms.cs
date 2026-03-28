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
    }

    public sealed class BillingWebhookOpsSummaryVm
    {
        public int ActiveSubscriptionCount { get; set; }
        public int PendingDeliveryCount { get; set; }
        public int FailedDeliveryCount { get; set; }
        public int SucceededDeliveryCount { get; set; }
        public int RetryPendingCount { get; set; }
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
    }

    public sealed class RefundOpsSummaryVm
    {
        public int PendingCount { get; set; }
        public int CompletedCount { get; set; }
        public int FailedCount { get; set; }
        public int StripeCount { get; set; }
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
        public string Currency { get; set; } = "EUR";
        public PaymentStatus Status { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? ProviderTransactionRef { get; set; }
        public string? FailureReason { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public long RefundedAmountMinor { get; set; }
        public long NetCapturedAmountMinor { get; set; }
        public bool IsStripe { get; set; }
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
        public PaymentStatus PaymentStatus { get; set; }
        public Guid? CustomerId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = "EUR";
        public string Reason { get; set; } = string.Empty;
        public RefundStatus Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public bool IsStripe { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class PaymentEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public DateTime CreatedAtUtc { get; set; }
        public bool IsStripe { get; set; }
        public string? FailureReason { get; set; }

        [Required]
        public Guid BusinessId { get; set; }

        [Display(Name = "Order id")]
        public Guid? OrderId { get; set; }
        public string? OrderNumber { get; set; }

        [Display(Name = "Invoice id")]
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
        public string Currency { get; set; } = "EUR";

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [Required]
        [StringLength(120)]
        public string Provider { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ProviderTransactionRef { get; set; }

        public DateTime? PaidAtUtc { get; set; }
        public long RefundedAmountMinor { get; set; }
        public long NetCapturedAmountMinor { get; set; }
        public List<PaymentRefundHistoryItemVm> Refunds { get; set; } = new();
        public List<PaymentSupportPlaybookVm> SupportPlaybooks { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> CustomerOptions { get; set; } = new();
        public List<SelectListItem> UserOptions { get; set; } = new();
    }

    public sealed class PaymentRefundHistoryItemVm
    {
        public Guid Id { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = "EUR";
        public string Reason { get; set; } = string.Empty;
        public RefundStatus Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
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
        public long AmountMinor { get; set; }
        public DateTime ExpenseDateUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
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

        public DateTime ExpenseDateUtc { get; set; } = DateTime.UtcNow.Date;
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> SupplierOptions { get; set; } = new();
    }

    public sealed class JournalEntriesListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public JournalEntryQueueFilter? QueueFilter { get; set; }
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<JournalEntryListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class JournalEntryListItemVm
    {
        public Guid Id { get; set; }
        public DateTime EntryDateUtc { get; set; }
        public string Description { get; set; } = string.Empty;
        public int LineCount { get; set; }
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

        public DateTime EntryDateUtc { get; set; } = DateTime.UtcNow.Date;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public List<JournalEntryLineVm> Lines { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> AccountOptions { get; set; } = new();
    }
}
