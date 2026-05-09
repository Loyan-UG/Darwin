using Darwin.Domain.Enums;
using Darwin.Application.Settings.DTOs;

namespace Darwin.Application.Billing.DTOs
{
    public enum PaymentQueueFilter : short
    {
        Pending = 1,
        Failed = 2,
        Refunded = 3,
        Unlinked = 4,
        ProviderLinked = 5,
        Stripe = 6,
        MissingProviderRef = 7,
        FailedStripe = 8,
        NeedsReconciliation = 9,
        DisputeFollowUp = 10
    }

    public enum BillingRefundQueueFilter : short
    {
        Pending = 1,
        Completed = 2,
        Failed = 3,
        Stripe = 4,
        NeedsSupport = 5
    }

    public enum JournalEntryQueueFilter : short
    {
        Recent = 1,
        MultiLine = 2
    }

    public sealed class PaymentListItemDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
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
        public string Currency { get; set; } = SiteSettingDto.DefaultCurrencyDefault;
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

    public sealed class PaymentOpsSummaryDto
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

    public sealed class TaxComplianceOpsSummaryDto
    {
        public int BusinessCustomersMissingVatIdCount { get; set; }
        public int BusinessCustomersVatValidationReviewCount { get; set; }
        public int BusinessInvoicesMissingVatIdCount { get; set; }
        public int ReverseChargeCandidateInvoiceCount { get; set; }
        public int DraftInvoiceCount { get; set; }
        public int DueSoonInvoiceCount { get; set; }
        public int OverdueInvoiceCount { get; set; }
    }

    public sealed class TaxComplianceInvoiceReviewItemDto
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
        public string Currency { get; set; } = SiteSettingDto.DefaultCurrencyDefault;
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

    public sealed class TaxComplianceCustomerReviewItemDto
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

    public sealed class TaxComplianceOverviewDto
    {
        public TaxComplianceOpsSummaryDto Summary { get; set; } = new();
        public List<TaxComplianceInvoiceReviewItemDto> InvoiceItems { get; set; } = new();
        public List<TaxComplianceCustomerReviewItemDto> CustomerItems { get; set; } = new();
    }

    public sealed class TaxComplianceInvoiceExportRowDto
    {
        public Guid InvoiceId { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public CustomerTaxProfileType? CustomerTaxProfileType { get; set; }
        public string? CustomerVatId { get; set; }
        public Guid? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public Guid? PaymentId { get; set; }
        public InvoiceStatus Status { get; set; }
        public string Currency { get; set; } = SiteSettingDto.DefaultCurrencyDefault;
        public long TotalNetMinor { get; set; }
        public long TotalTaxMinor { get; set; }
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

    public sealed class BillingRefundListItemDto
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
        public Guid? CustomerId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = SiteSettingDto.DefaultCurrencyDefault;
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

    public sealed class RefundOpsSummaryDto
    {
        public int PendingCount { get; set; }
        public int CompletedCount { get; set; }
        public int FailedCount { get; set; }
        public int StripeCount { get; set; }
        public int NeedsSupportCount { get; set; }
    }

    public class PaymentCreateDto
    {
        public Guid BusinessId { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? InvoiceId { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? UserId { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = SiteSettingDto.DefaultCurrencyDefault;
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string Provider { get; set; } = string.Empty;
        public string? ProviderTransactionRef { get; set; }
        public string? ProviderPaymentIntentRef { get; set; }
        public string? ProviderCheckoutSessionRef { get; set; }
        public DateTime? PaidAtUtc { get; set; }
    }

    public sealed class PaymentEditDto : PaymentCreateDto
    {
        public Guid Id { get; set; }
        public string? FailureReason { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public bool IsStripe { get; set; }
        public DateTime? LastFinancialEventAtUtc { get; set; }
        public int OpenAgeHours { get; set; }
        public string ProviderReferenceState { get; set; } = string.Empty;
        public string DisputeReviewState { get; set; } = string.Empty;
        public bool NeedsReconciliation { get; set; }
        public bool NeedsDisputeFollowUp { get; set; }
        public string? OrderNumber { get; set; }
        public InvoiceStatus? InvoiceStatus { get; set; }
        public DateTime? InvoiceDueAtUtc { get; set; }
        public long? InvoiceTotalGrossMinor { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string UserDisplayName { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public long RefundedAmountMinor { get; set; }
        public long NetCapturedAmountMinor { get; set; }
        public List<PaymentRefundHistoryItemDto> Refunds { get; set; } = new();
        public List<PaymentProviderEventItemDto> ProviderEvents { get; set; } = new();
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class UpdatePaymentDisputeReviewDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Action { get; set; } = string.Empty;
        public string? Note { get; set; }
    }

    public sealed class PaymentRefundHistoryItemDto
    {
        public Guid Id { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = SiteSettingDto.DefaultCurrencyDefault;
        public string Reason { get; set; } = string.Empty;
        public RefundStatus Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }

    public sealed class PaymentProviderEventItemDto
    {
        public string EventType { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }
        public string? IdempotencyKey { get; set; }
        public string CorrelationKind { get; set; } = string.Empty;
        public string CorrelationReference { get; set; } = string.Empty;
    }

    public sealed class FinancialAccountListItemDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        public string? Code { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class FinancialAccountOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int AssetCount { get; set; }
        public int RevenueCount { get; set; }
        public int ExpenseCount { get; set; }
        public int MissingCodeCount { get; set; }
    }

    public class FinancialAccountCreateDto
    {
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; } = AccountType.Asset;
        public string? Code { get; set; }
    }

    public sealed class FinancialAccountEditDto : FinancialAccountCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class ExpenseListItemDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid? SupplierId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long AmountMinor { get; set; }
        public DateTime ExpenseDateUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class ExpenseOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int SupplierLinkedCount { get; set; }
        public int RecentCount { get; set; }
        public int HighValueCount { get; set; }
    }

    public class ExpenseCreateDto
    {
        public Guid BusinessId { get; set; }
        public Guid? SupplierId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long AmountMinor { get; set; }
        public DateTime ExpenseDateUtc { get; set; }
    }

    public sealed class ExpenseEditDto : ExpenseCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class JournalEntryLineDto
    {
        public Guid? Id { get; set; }
        public Guid AccountId { get; set; }
        public long DebitMinor { get; set; }
        public long CreditMinor { get; set; }
        public string? Memo { get; set; }
    }

    public sealed class JournalEntryListItemDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public DateTime EntryDateUtc { get; set; }
        public string Description { get; set; } = string.Empty;
        public int LineCount { get; set; }
        public long TotalDebitMinor { get; set; }
        public long TotalCreditMinor { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class JournalEntryOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int RecentCount { get; set; }
        public int MultiLineCount { get; set; }
    }

    public class JournalEntryCreateDto
    {
        public Guid BusinessId { get; set; }
        public DateTime EntryDateUtc { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<JournalEntryLineDto> Lines { get; set; } = new();
    }

    public sealed class JournalEntryEditDto : JournalEntryCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
