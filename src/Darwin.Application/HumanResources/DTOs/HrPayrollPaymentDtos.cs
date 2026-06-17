using Darwin.Domain.Enums;

namespace Darwin.Application.HumanResources.DTOs;

public enum PayrollPaymentQueueFilter
{
    All = 0,
    Draft = 1,
    Posted = 2,
    Cancelled = 3,
    Reversed = 4
}

public sealed class PayrollPaymentsPageDto
{
    public Guid? BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public List<(Guid Id, string Name)> BusinessOptions { get; set; } = new();
    public string Query { get; set; } = string.Empty;
    public PayrollPaymentQueueFilter Filter { get; set; } = PayrollPaymentQueueFilter.All;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int Total { get; set; }
    public int DraftCount { get; set; }
    public int PostedCount { get; set; }
    public int CancelledCount { get; set; }
    public int ReversedCount { get; set; }
    public List<PayrollPaymentListItemDto> Items { get; set; } = new();
}

public sealed class PayrollPaymentListItemDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid PayrollRunId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public string RunNumber { get; set; } = string.Empty;
    public PayrollPaymentStatus Status { get; set; } = PayrollPaymentStatus.Draft;
    public PayrollPaymentMethod PaymentMethod { get; set; } = PayrollPaymentMethod.BankTransfer;
    public DateTime PaymentDateUtc { get; set; }
    public string Currency { get; set; } = "EUR";
    public long TotalAmountMinor { get; set; }
    public int AllocationCount { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public string Reference { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class PayrollPaymentAllocationDto
{
    public Guid PayrollRunLineId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public long LineNetPayMinor { get; set; }
    public long AlreadyPaidMinor { get; set; }
    public long OpenAmountMinor { get; set; }
    public long AmountMinor { get; set; }
    public string? Memo { get; set; }
}

public class PayrollPaymentCreateDto
{
    public Guid BusinessId { get; set; }
    public Guid PayrollRunId { get; set; }
    public PayrollPaymentMethod PaymentMethod { get; set; } = PayrollPaymentMethod.BankTransfer;
    public DateTime PaymentDateUtc { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? Reference { get; set; }
    public string? InternalNotes { get; set; }
    public string? MetadataJson { get; set; } = "{}";
    public List<PayrollPaymentAllocationDto> Allocations { get; set; } = new();
}

public sealed class PayrollPaymentEditDto : PayrollPaymentCreateDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public string PaymentNumber { get; set; } = string.Empty;
    public PayrollPaymentStatus Status { get; set; } = PayrollPaymentStatus.Draft;
    public long TotalAmountMinor { get; set; }
    public Guid? PostingJournalEntryId { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public Guid? ReversalJournalEntryId { get; set; }
    public DateTime? ReversedAtUtc { get; set; }
    public string? ReversalReason { get; set; }
    public DateTime? BankSettledAtUtc { get; set; }
    public Guid? BankSettlementJournalEntryId { get; set; }
    public Guid? BankSettlementReconciliationMatchId { get; set; }
    public string? BankSettlementNotes { get; set; }
    public List<PayrollPaymentBankSettlementCandidateDto> BankSettlementCandidates { get; set; } = new();
    public List<PayrollPaymentBankSettlementCandidateDto> BankCorrectionCandidates { get; set; } = new();
    public List<PayrollPaymentBankCorrectionListItemDto> BankCorrections { get; set; } = new();
}

public sealed class PayrollPaymentBankSettlementActionDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BankReconciliationMatchId { get; set; }
    public string? Notes { get; set; }
}

public sealed class PayrollPaymentBankSettlementCandidateDto
{
    public Guid BankReconciliationMatchId { get; set; }
    public string MatchNumber { get; set; } = string.Empty;
    public Guid BankAccountId { get; set; }
    public string BankAccountDisplayName { get; set; } = string.Empty;
    public DateTime MatchDateUtc { get; set; }
    public string Currency { get; set; } = "EUR";
    public long BankTotalMinor { get; set; }
    public long FinanceTotalMinor { get; set; }
}

public sealed class PayrollPaymentLifecycleActionDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public string? Reason { get; set; }
}

public sealed class PayrollPaymentBankCorrectionListItemDto
{
    public Guid Id { get; set; }
    public Guid PayrollPaymentId { get; set; }
    public Guid BankReconciliationMatchId { get; set; }
    public Guid? BankStatementLineId { get; set; }
    public Guid? CorrectionJournalEntryId { get; set; }
    public PayrollPaymentBankCorrectionType CorrectionType { get; set; }
    public PayrollPaymentBankCorrectionStatus Status { get; set; }
    public DateTime CorrectionDateUtc { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public string Currency { get; set; } = "EUR";
    public long AmountMinor { get; set; }
    public string Reason { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class PayrollPaymentBankCorrectionActionDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public string? Reason { get; set; }
}

public sealed class PayrollPaymentBankCorrectionCreateDto
{
    public Guid PayrollPaymentId { get; set; }
    public byte[] PayrollPaymentRowVersion { get; set; } = Array.Empty<byte>();
    public PayrollPaymentBankCorrectionType CorrectionType { get; set; } = PayrollPaymentBankCorrectionType.ReturnedTransfer;
    public Guid BankReconciliationMatchId { get; set; }
    public Guid? BankStatementLineId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? InternalNotes { get; set; }
}
