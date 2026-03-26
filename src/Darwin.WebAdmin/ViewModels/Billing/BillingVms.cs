using Darwin.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Darwin.WebAdmin.ViewModels.Billing
{
    public sealed class PaymentsListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<PaymentListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
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
        public DateTime? PaidAtUtc { get; set; }
        public long RefundedAmountMinor { get; set; }
        public long NetCapturedAmountMinor { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class PaymentEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

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
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> CustomerOptions { get; set; } = new();
        public List<SelectListItem> UserOptions { get; set; } = new();
    }

    public sealed class FinancialAccountsListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
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
