using Darwin.Domain.Enums;

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
        NeedsReconciliation = 9
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
        public string Currency { get; set; } = "EUR";
        public PaymentStatus Status { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? ProviderTransactionRef { get; set; }
        public string? FailureReason { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public long RefundedAmountMinor { get; set; }
        public long NetCapturedAmountMinor { get; set; }
        public DateTime? LastFinancialEventAtUtc { get; set; }
        public int OpenAgeHours { get; set; }
        public string ProviderReferenceState { get; set; } = string.Empty;
        public bool IsStripe { get; set; }
        public bool NeedsReconciliation { get; set; }
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
    }

    public sealed class BillingRefundListItemDto
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
        public string Currency { get; set; } = "EUR";
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string Provider { get; set; } = string.Empty;
        public string? ProviderTransactionRef { get; set; }
        public DateTime? PaidAtUtc { get; set; }
    }

    public sealed class PaymentEditDto : PaymentCreateDto
    {
        public Guid Id { get; set; }
        public string? FailureReason { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public bool IsStripe { get; set; }
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
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class PaymentRefundHistoryItemDto
    {
        public Guid Id { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = "EUR";
        public string Reason { get; set; } = string.Empty;
        public RefundStatus Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
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
