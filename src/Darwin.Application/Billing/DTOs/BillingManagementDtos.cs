using Darwin.Domain.Enums;

namespace Darwin.Application.Billing.DTOs
{
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
        public DateTime? PaidAtUtc { get; set; }
        public long RefundedAmountMinor { get; set; }
        public long NetCapturedAmountMinor { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
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
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
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
