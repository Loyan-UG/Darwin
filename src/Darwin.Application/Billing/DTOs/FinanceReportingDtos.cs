using Darwin.Domain.Enums;

namespace Darwin.Application.Billing.DTOs;

public sealed class FinanceBusinessOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class FinanceOverviewDto
{
    public Guid? BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Currency { get; set; } = "EUR";
    public List<FinanceBusinessOptionDto> BusinessOptions { get; set; } = new();
    public long ReceivablesDebitMinor { get; set; }
    public long ReceivablesCreditMinor { get; set; }
    public long OpenReceivablesMinor { get; set; }
    public string? ReceivablesReadinessMessage { get; set; }
    public int PostedJournalEntryCount { get; set; }
    public int DraftJournalEntryCount { get; set; }
    public int ReversedJournalEntryCount { get; set; }
    public int SourceLinkedPostingCount { get; set; }
    public int MissingSourcePostingCount { get; set; }
    public int IssuedCreditNoteCount { get; set; }
    public int UnpostedIssuedCreditNoteCount { get; set; }
    public List<FinancePostingKindBreakdownDto> PostingKindBreakdown { get; set; } = new();
    public List<FinanceReceivableSourceDto> TopReceivables { get; set; } = new();
    public List<FinancePostingListItemDto> RecentPostings { get; set; } = new();
}

public sealed class FinancePostingKindBreakdownDto
{
    public JournalEntryPostingKind PostingKind { get; set; }
    public int Count { get; set; }
    public long DebitMinor { get; set; }
    public long CreditMinor { get; set; }
}

public sealed class FinanceReceivablesPageDto
{
    public Guid? BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Currency { get; set; } = "EUR";
    public string Query { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int Total { get; set; }
    public long TotalDebitMinor { get; set; }
    public long TotalCreditMinor { get; set; }
    public long OpenBalanceMinor { get; set; }
    public string? ReadinessMessage { get; set; }
    public List<FinanceBusinessOptionDto> BusinessOptions { get; set; } = new();
    public List<FinanceReceivableSourceDto> Items { get; set; } = new();
}

public sealed class FinanceReceivableSourceDto
{
    public string SourceEntityType { get; set; } = string.Empty;
    public Guid? SourceEntityId { get; set; }
    public string SourceDocumentNumber { get; set; } = string.Empty;
    public long DebitMinor { get; set; }
    public long CreditMinor { get; set; }
    public long OpenBalanceMinor { get; set; }
    public DateTime LastEntryDateUtc { get; set; }
    public JournalEntryPostingKind LastPostingKind { get; set; }
    public JournalEntryPostingStatus LastPostingStatus { get; set; }
    public string LastPostingKey { get; set; } = string.Empty;
}

public sealed class FinancePostingsPageDto
{
    public Guid? BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public JournalEntryPostingKind? PostingKind { get; set; }
    public JournalEntryPostingStatus? PostingStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int Total { get; set; }
    public List<FinanceBusinessOptionDto> BusinessOptions { get; set; } = new();
    public List<FinancePostingListItemDto> Items { get; set; } = new();
}

public sealed class FinanceAccountMappingsPageDto
{
    public Guid? BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public List<FinanceBusinessOptionDto> BusinessOptions { get; set; } = new();
    public List<FinanceAccountMappingRowDto> Rows { get; set; } = new();
    public int MissingRequiredMappingCount => Rows.Count(x => (x.IsRequiredForSalesPosting || x.IsRequiredForPayablesPosting) && (!x.MappingId.HasValue || !x.IsActive));
    public int IncompatibleMappingCount => Rows.Count(x => x.MappingId.HasValue && !x.IsCompatible);
}

public sealed class FinanceAccountMappingRowDto
{
    public Guid? MappingId { get; set; }
    public FinancePostingAccountRole Role { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string AllowedAccountTypesLabel { get; set; } = string.Empty;
    public Guid? FinancialAccountId { get; set; }
    public string FinancialAccountName { get; set; } = string.Empty;
    public string FinancialAccountCode { get; set; } = string.Empty;
    public AccountType? FinancialAccountType { get; set; }
    public bool IsActive { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsCompatible { get; set; } = true;
    public bool IsRequiredForSalesPosting { get; set; }
    public bool IsRequiredForPayablesPosting { get; set; }
    public List<FinanceAccountOptionDto> CompatibleAccountOptions { get; set; } = new();
}

public sealed class FinanceAccountOptionDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public AccountType Type { get; set; }
}

public sealed class FinanceAccountMappingUpsertDto
{
    public Guid BusinessId { get; set; }
    public FinancePostingAccountRole Role { get; set; }
    public Guid FinancialAccountId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public sealed class FinanceExportsPageDto
{
    public Guid? BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public List<FinanceBusinessOptionDto> BusinessOptions { get; set; } = new();
    public Guid? ExternalSystemId { get; set; }
    public List<FinanceExportTargetOptionDto> ExternalSystemOptions { get; set; } = new();
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public FinanceExportPostingStatusMode PostingStatusMode { get; set; } = FinanceExportPostingStatusMode.PostedAndReversed;
    public bool StorageReady { get; set; }
    public string StorageReadinessMessage { get; set; } = string.Empty;
    public bool ConnectorAdapterReady { get; set; }
    public string ConnectorReadinessMessage { get; set; } = string.Empty;
    public int DraftBatchCount { get; set; }
    public int FailedBatchCount { get; set; }
    public int GeneratedBatchCount { get; set; }
    public int DeliveredBatchCount { get; set; }
    public List<FinanceExportBatchListItemDto> Items { get; set; } = new();
}

public sealed class FinanceExportTargetOptionDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public sealed class FinanceExportBatchListItemDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid ExternalSystemId { get; set; }
    public string ExternalSystemName { get; set; } = string.Empty;
    public string ExportKey { get; set; } = string.Empty;
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public FinanceExportPostingStatusMode PostingStatusMode { get; set; }
    public FinanceExportBatchStatus Status { get; set; }
    public DateTime? GeneratedAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public string PackageHashSha256 { get; set; } = string.Empty;
    public string PackageFileName { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public FinanceExportAttemptStatus? LastAttemptStatus { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public string ErrorSummary { get; set; } = string.Empty;
    public bool HasPackageDocument { get; set; }
    public bool CanPush { get; set; }
    public bool HasDeliveryReference { get; set; }
    public string DeliveryReferenceDisplay { get; set; } = string.Empty;
    public bool CanGenerate => Status is FinanceExportBatchStatus.Draft or FinanceExportBatchStatus.Failed;
    public bool CanDownload => HasPackageDocument && Status is (FinanceExportBatchStatus.Generated or FinanceExportBatchStatus.Delivered);
}

public sealed class FinanceExportBatchCreateDto
{
    public Guid BusinessId { get; set; }
    public Guid ExternalSystemId { get; set; }
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public FinanceExportPostingStatusMode PostingStatusMode { get; set; } = FinanceExportPostingStatusMode.PostedAndReversed;
}

public sealed class SupplierInvoicesPageDto
{
    public Guid? BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public SupplierInvoiceStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int Total { get; set; }
    public int DraftCount { get; set; }
    public int MatchedCount { get; set; }
    public int ApprovedCount { get; set; }
    public int PostedCount { get; set; }
    public int VoidedCount { get; set; }
    public List<FinanceBusinessOptionDto> BusinessOptions { get; set; } = new();
    public List<SupplierInvoiceListItemDto> Items { get; set; } = new();
}

public sealed class SupplierInvoiceListItemDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierInvoiceNumber { get; set; } = string.Empty;
    public string InternalInvoiceNumber { get; set; } = string.Empty;
    public SupplierInvoiceStatus Status { get; set; }
    public DateTime InvoiceDateUtc { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public string Currency { get; set; } = "EUR";
    public long TotalNetMinor { get; set; }
    public long TotalTaxMinor { get; set; }
    public long TotalGrossMinor { get; set; }
    public int LineCount { get; set; }
    public int DiscrepancyCount { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class SupplierInvoiceLineDto
{
    public Guid Id { get; set; }
    public Guid? PurchaseOrderLineId { get; set; }
    public Guid? GoodsReceiptLineId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string? SupplierSku { get; set; }
    public string Description { get; set; } = string.Empty;
    public int InvoicedQuantity { get; set; }
    public long UnitNetMinor { get; set; }
    public long UnitTaxMinor { get; set; }
    public long UnitGrossMinor { get; set; }
    public long TotalNetMinor { get; set; }
    public long TotalTaxMinor { get; set; }
    public long TotalGrossMinor { get; set; }
    public decimal TaxRate { get; set; }
    public SupplierInvoiceLineMatchStatus MatchStatus { get; set; } = SupplierInvoiceLineMatchStatus.Unmatched;
    public string? DiscrepancyReason { get; set; }
    public int SortOrder { get; set; }
}

public class SupplierInvoiceCreateDto
{
    public Guid BusinessId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public string SupplierInvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDateUtc { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public int? PaymentTermDays { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? InternalNotes { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public List<SupplierInvoiceLineDto> Lines { get; set; } = new();
}

public sealed class SupplierInvoiceEditDto : SupplierInvoiceCreateDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public string InternalInvoiceNumber { get; set; } = string.Empty;
    public SupplierInvoiceStatus Status { get; set; } = SupplierInvoiceStatus.Draft;
    public DateTime? MatchedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public DateTime? VoidedAtUtc { get; set; }
    public Guid? PostingJournalEntryId { get; set; }
    public long TotalNetMinor { get; set; }
    public long TotalTaxMinor { get; set; }
    public long TotalGrossMinor { get; set; }
}

public sealed class SupplierInvoiceLifecycleActionDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public string Action { get; set; } = string.Empty;
}

public sealed class SupplierPaymentsPageDto
{
    public Guid? BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public SupplierPaymentStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int Total { get; set; }
    public int DraftCount { get; set; }
    public int PostedCount { get; set; }
    public int CancelledCount { get; set; }
    public int ReversedCount { get; set; }
    public List<FinanceBusinessOptionDto> BusinessOptions { get; set; } = new();
    public List<SupplierPaymentListItemDto> Items { get; set; } = new();
}

public sealed class SupplierPaymentListItemDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string PaymentNumber { get; set; } = string.Empty;
    public SupplierPaymentStatus Status { get; set; }
    public SupplierPaymentMethod PaymentMethod { get; set; }
    public DateTime PaymentDateUtc { get; set; }
    public string Currency { get; set; } = "EUR";
    public long TotalAmountMinor { get; set; }
    public int AllocationCount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime? BankSettledAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class SupplierPaymentAllocationDto
{
    public Guid SupplierInvoiceId { get; set; }
    public string SupplierInvoiceNumber { get; set; } = string.Empty;
    public string InternalInvoiceNumber { get; set; } = string.Empty;
    public DateTime? DueDateUtc { get; set; }
    public long InvoiceGrossMinor { get; set; }
    public long AlreadyPaidMinor { get; set; }
    public long OpenAmountMinor { get; set; }
    public long AmountMinor { get; set; }
    public string? Memo { get; set; }
}

public class SupplierPaymentCreateDto
{
    public Guid BusinessId { get; set; }
    public Guid SupplierId { get; set; }
    public SupplierPaymentMethod PaymentMethod { get; set; } = SupplierPaymentMethod.BankTransfer;
    public DateTime PaymentDateUtc { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? Reference { get; set; }
    public string? InternalNotes { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public List<SupplierPaymentAllocationDto> Allocations { get; set; } = new();
}

public sealed class SupplierPaymentEditDto : SupplierPaymentCreateDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public string PaymentNumber { get; set; } = string.Empty;
    public SupplierPaymentStatus Status { get; set; } = SupplierPaymentStatus.Draft;
    public long TotalAmountMinor { get; set; }
    public Guid? PostingJournalEntryId { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public Guid? ReversalJournalEntryId { get; set; }
    public DateTime? ReversedAtUtc { get; set; }
    public string? ReversalReason { get; set; }
    public Guid? BankSettlementJournalEntryId { get; set; }
    public Guid? BankSettlementReconciliationMatchId { get; set; }
    public DateTime? BankSettledAtUtc { get; set; }
    public string? BankSettlementNotes { get; set; }
    public List<SupplierPaymentBankSettlementCandidateDto> BankSettlementCandidates { get; set; } = new();
    public List<SupplierPaymentBankSettlementCandidateDto> BankCorrectionCandidates { get; set; } = new();
    public List<SupplierPaymentBankCorrectionListItemDto> BankCorrections { get; set; } = new();
    public DateTime? CancelledAtUtc { get; set; }
}

public sealed class SupplierPaymentLifecycleActionDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public string Action { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public sealed class SupplierPaymentBankSettlementActionDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BankReconciliationMatchId { get; set; }
    public string? Notes { get; set; }
}

public sealed class SupplierPaymentBankSettlementCandidateDto
{
    public Guid BankReconciliationMatchId { get; set; }
    public string MatchNumber { get; set; } = string.Empty;
    public Guid BankAccountId { get; set; }
    public string BankAccountDisplayName { get; set; } = string.Empty;
    public DateTime MatchDateUtc { get; set; }
    public string Currency { get; set; } = string.Empty;
    public long BankTotalMinor { get; set; }
    public long FinanceTotalMinor { get; set; }
}

public sealed class SupplierPaymentBankCorrectionListItemDto
{
    public Guid Id { get; set; }
    public Guid SupplierPaymentId { get; set; }
    public Guid BankReconciliationMatchId { get; set; }
    public Guid? BankStatementLineId { get; set; }
    public Guid? CorrectionJournalEntryId { get; set; }
    public SupplierPaymentBankCorrectionType CorrectionType { get; set; }
    public SupplierPaymentBankCorrectionStatus Status { get; set; }
    public DateTime CorrectionDateUtc { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public string Currency { get; set; } = "EUR";
    public long AmountMinor { get; set; }
    public string Reason { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class SupplierPaymentBankCorrectionActionDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public string? Reason { get; set; }
}

public sealed class SupplierPaymentBankCorrectionCreateDto
{
    public Guid SupplierPaymentId { get; set; }
    public byte[] SupplierPaymentRowVersion { get; set; } = Array.Empty<byte>();
    public SupplierPaymentBankCorrectionType CorrectionType { get; set; } = SupplierPaymentBankCorrectionType.ReturnedTransfer;
    public Guid BankReconciliationMatchId { get; set; }
    public Guid? BankStatementLineId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? InternalNotes { get; set; }
}

public sealed class SupplierAdvancesPageDto
{
    public Guid? BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public SupplierAdvanceStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int Total { get; set; }
    public int DraftCount { get; set; }
    public int PostedCount { get; set; }
    public int AppliedCount { get; set; }
    public int CancelledCount { get; set; }
    public int ReversedCount { get; set; }
    public List<FinanceBusinessOptionDto> BusinessOptions { get; set; } = new();
    public List<SupplierAdvanceListItemDto> Items { get; set; } = new();
}

public sealed class SupplierAdvanceListItemDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string AdvanceNumber { get; set; } = string.Empty;
    public SupplierAdvanceStatus Status { get; set; }
    public SupplierPaymentMethod PaymentMethod { get; set; }
    public DateTime AdvanceDateUtc { get; set; }
    public string Currency { get; set; } = "EUR";
    public long TotalAmountMinor { get; set; }
    public long OpenAmountMinor { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int ApplicationCount { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class SupplierAdvanceApplicationDto
{
    public Guid Id { get; set; }
    public Guid SupplierInvoiceId { get; set; }
    public string SupplierInvoiceNumber { get; set; } = string.Empty;
    public string InternalInvoiceNumber { get; set; } = string.Empty;
    public DateTime? DueDateUtc { get; set; }
    public long InvoiceGrossMinor { get; set; }
    public long AlreadySettledMinor { get; set; }
    public long InvoiceOpenAmountMinor { get; set; }
    public long AmountMinor { get; set; }
    public string? Memo { get; set; }
    public Guid? PostingJournalEntryId { get; set; }
    public Guid? ReversalJournalEntryId { get; set; }
    public DateTime? AppliedAtUtc { get; set; }
    public DateTime? ReversedAtUtc { get; set; }
    public string? ReversalReason { get; set; }
}

public class SupplierAdvanceCreateDto
{
    public Guid BusinessId { get; set; }
    public Guid SupplierId { get; set; }
    public SupplierPaymentMethod PaymentMethod { get; set; } = SupplierPaymentMethod.BankTransfer;
    public DateTime AdvanceDateUtc { get; set; }
    public string Currency { get; set; } = "EUR";
    public long TotalAmountMinor { get; set; }
    public string? Reference { get; set; }
    public string? InternalNotes { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public sealed class SupplierAdvanceEditDto : SupplierAdvanceCreateDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public string AdvanceNumber { get; set; } = string.Empty;
    public SupplierAdvanceStatus Status { get; set; } = SupplierAdvanceStatus.Draft;
    public long OpenAmountMinor { get; set; }
    public Guid? PostingJournalEntryId { get; set; }
    public Guid? ReversalJournalEntryId { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public DateTime? ReversedAtUtc { get; set; }
    public string? ReversalReason { get; set; }
    public List<SupplierAdvanceApplicationDto> Applications { get; set; } = new();
    public List<SupplierAdvanceApplicationDto> ApplicationCandidates { get; set; } = new();
}

public sealed class SupplierAdvanceLifecycleActionDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public string? Reason { get; set; }
}

public sealed class SupplierAdvanceApplyDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid SupplierInvoiceId { get; set; }
    public long AmountMinor { get; set; }
    public string? Memo { get; set; }
}

public sealed class SupplierAdvanceApplicationReverseDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid ApplicationId { get; set; }
    public string? Reason { get; set; }
}

public sealed class BankAccountsPageDto
{
    public Guid? BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public BankAccountStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int Total { get; set; }
    public int ActiveCount { get; set; }
    public int ArchivedCount { get; set; }
    public List<FinanceBusinessOptionDto> BusinessOptions { get; set; } = new();
    public List<BankAccountListItemDto> Items { get; set; } = new();
}

public sealed class BankAccountListItemDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? FinancialAccountId { get; set; }
    public string FinancialAccountLabel { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string Currency { get; set; } = "EUR";
    public string MaskedAccountIdentifier { get; set; } = string.Empty;
    public BankAccountStatus Status { get; set; }
    public bool IsDefault { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class BankAccountCreateDto
{
    public Guid BusinessId { get; set; }
    public Guid? FinancialAccountId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? MaskedAccountIdentifier { get; set; }
    public bool IsDefault { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public sealed class BankAccountEditDto : BankAccountCreateDto
{
    public Guid Id { get; set; }
    public BankAccountStatus Status { get; set; } = BankAccountStatus.Active;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class BankStatementsPageDto
{
    public Guid? BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public Guid? BankAccountId { get; set; }
    public string Query { get; set; } = string.Empty;
    public BankStatementImportStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int Total { get; set; }
    public int ImportedCount { get; set; }
    public int CancelledCount { get; set; }
    public List<FinanceBusinessOptionDto> BusinessOptions { get; set; } = new();
    public List<BankAccountOptionDto> BankAccountOptions { get; set; } = new();
    public List<BankStatementImportListItemDto> Items { get; set; } = new();
}

public sealed class BankAccountOptionDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public sealed class BankStatementImportListItemDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid BankAccountId { get; set; }
    public string BankAccountLabel { get; set; } = string.Empty;
    public string StatementReference { get; set; } = string.Empty;
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public DateTime ImportedAtUtc { get; set; }
    public BankStatementImportStatus Status { get; set; }
    public int LineCount { get; set; }
    public long DebitTotalMinor { get; set; }
    public long CreditTotalMinor { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class BankStatementLineDto
{
    public Guid Id { get; set; }
    public DateTime TransactionDateUtc { get; set; }
    public DateTime? ValueDateUtc { get; set; }
    public BankStatementLineDirection Direction { get; set; }
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? CounterpartyName { get; set; }
    public string? CounterpartyReference { get; set; }
    public string? RemittanceInformation { get; set; }
    public string NormalizedIdentityKey { get; set; } = string.Empty;
    public BankStatementLineReviewStatus ReviewStatus { get; set; } = BankStatementLineReviewStatus.Unreviewed;
    public string MetadataJson { get; set; } = "{}";
}

public class BankStatementImportCreateDto
{
    public Guid BusinessId { get; set; }
    public Guid BankAccountId { get; set; }
    public string StatementReference { get; set; } = string.Empty;
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public List<BankStatementLineDto> Lines { get; set; } = new();
}

public sealed class BankStatementImportDetailDto : BankStatementImportCreateDto
{
    public Guid Id { get; set; }
    public string BankAccountLabel { get; set; } = string.Empty;
    public DateTime ImportedAtUtc { get; set; }
    public BankStatementImportStatus Status { get; set; }
    public int LineCount { get; set; }
    public long DebitTotalMinor { get; set; }
    public long CreditTotalMinor { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class BankStatementImportLifecycleActionDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class BankReconciliationPageDto
{
    public Guid? BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public Guid? BankAccountId { get; set; }
    public string Query { get; set; } = string.Empty;
    public BankReconciliationMatchStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int Total { get; set; }
    public int DraftCount { get; set; }
    public int MatchedCount { get; set; }
    public int CancelledCount { get; set; }
    public List<FinanceBusinessOptionDto> BusinessOptions { get; set; } = new();
    public List<BankAccountOptionDto> BankAccountOptions { get; set; } = new();
    public List<BankReconciliationListItemDto> Items { get; set; } = new();
}

public sealed class BankReconciliationListItemDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid BankAccountId { get; set; }
    public string BankAccountLabel { get; set; } = string.Empty;
    public string MatchNumber { get; set; } = string.Empty;
    public BankReconciliationMatchStatus Status { get; set; }
    public DateTime MatchDateUtc { get; set; }
    public string Currency { get; set; } = "EUR";
    public long BankTotalMinor { get; set; }
    public long FinanceTotalMinor { get; set; }
    public long DifferenceMinor { get; set; }
    public int LineCount { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class BankReconciliationLineDto
{
    public Guid Id { get; set; }
    public Guid BankStatementLineId { get; set; }
    public Guid? JournalEntryId { get; set; }
    public BankReconciliationSourceType? SourceType { get; set; }
    public string? SourceEntityType { get; set; }
    public Guid? SourceEntityId { get; set; }
    public BankStatementLineDirection Direction { get; set; }
    public long AmountMinor { get; set; }
    public string? Memo { get; set; }
    public int SortOrder { get; set; }
    public string StatementLineLabel { get; set; } = string.Empty;
    public string PostingLabel { get; set; } = string.Empty;
}

public class BankReconciliationCreateDto
{
    public Guid BusinessId { get; set; }
    public Guid BankAccountId { get; set; }
    public string? MatchNumber { get; set; }
    public DateTime MatchDateUtc { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? ReviewNotes { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public List<BankReconciliationLineDto> Lines { get; set; } = new();
}

public sealed class BankReconciliationEditDto : BankReconciliationCreateDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public BankReconciliationMatchStatus Status { get; set; } = BankReconciliationMatchStatus.Draft;
    public long BankTotalMinor { get; set; }
    public long FinanceTotalMinor { get; set; }
    public long DifferenceMinor { get; set; }
    public DateTime? MatchedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public List<BankAccountOptionDto> BankAccountOptions { get; set; } = new();
    public List<BankReconciliationCandidateStatementLineDto> StatementLineCandidates { get; set; } = new();
    public List<BankReconciliationCandidatePostingDto> PostingCandidates { get; set; } = new();
}

public sealed class BankReconciliationLifecycleActionDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class BankReconciliationCandidateStatementLineDto
{
    public Guid Id { get; set; }
    public DateTime TransactionDateUtc { get; set; }
    public BankStatementLineDirection Direction { get; set; }
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Label { get; set; } = string.Empty;
}

public sealed class BankReconciliationCandidatePostingDto
{
    public Guid Id { get; set; }
    public DateTime EntryDateUtc { get; set; }
    public JournalEntryPostingKind PostingKind { get; set; }
    public JournalEntryPostingStatus PostingStatus { get; set; }
    public string PostingKey { get; set; } = string.Empty;
    public string SourceEntityType { get; set; } = string.Empty;
    public Guid? SourceEntityId { get; set; }
    public string SourceDocumentNumber { get; set; } = string.Empty;
    public long DebitMinor { get; set; }
    public long CreditMinor { get; set; }
    public string Label { get; set; } = string.Empty;
}

public sealed class FinancePostingListItemDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public DateTime EntryDateUtc { get; set; }
    public string Description { get; set; } = string.Empty;
    public JournalEntryPostingStatus PostingStatus { get; set; }
    public JournalEntryPostingKind PostingKind { get; set; }
    public string PostingKey { get; set; } = string.Empty;
    public string SourceEntityType { get; set; } = string.Empty;
    public Guid? SourceEntityId { get; set; }
    public string SourceDocumentNumber { get; set; } = string.Empty;
    public long DebitMinor { get; set; }
    public long CreditMinor { get; set; }
    public int LineCount { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public DateTime? ReversedAtUtc { get; set; }
    public bool IsBalanced => DebitMinor == CreditMinor && DebitMinor > 0;
}
