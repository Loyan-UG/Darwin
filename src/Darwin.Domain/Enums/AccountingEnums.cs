namespace Darwin.Domain.Enums
{
    /// <summary>
    /// Represents the classification of a financial account in a lightweight chart of accounts.
    /// </summary>
    public enum AccountType : short
    {
        Asset = 0,
        Liability = 1,
        Equity = 2,
        Revenue = 3,
        Expense = 4
    }

    /// <summary>
    /// Represents the lifecycle state of a journal entry for finance posting.
    /// </summary>
    public enum JournalEntryPostingStatus : short
    {
        Draft = 0,
        Posted = 1,
        Reversed = 2,
        Voided = 3
    }

    /// <summary>
    /// Represents the business source category that created a posted journal entry.
    /// </summary>
    public enum JournalEntryPostingKind : short
    {
        Manual = 0,
        InvoiceIssued = 1,
        PaymentRecorded = 2,
        RefundRecorded = 3,
        CreditNoteIssued = 4,
        Reversal = 5,
        Adjustment = 6,
        Import = 7,
        SupplierInvoicePosted = 8
    }

    /// <summary>
    /// Represents finance account roles used by automated sales and receivables postings.
    /// </summary>
    public enum FinancePostingAccountRole : short
    {
        Receivables = 0,
        SalesRevenue = 1,
        TaxPayable = 2,
        CashClearing = 3,
        RefundClearing = 4,
        Rounding = 5,
        AccountsPayable = 6,
        PurchaseExpense = 7,
        InventoryClearing = 8,
        TaxReceivable = 9
    }

    /// <summary>
    /// Represents the lifecycle state of a finance export batch.
    /// </summary>
    public enum FinanceExportBatchStatus : short
    {
        Draft = 0,
        Generated = 1,
        Delivered = 2,
        Failed = 3,
        Cancelled = 4
    }

    /// <summary>
    /// Represents which posted accounting entries are eligible for an export batch.
    /// </summary>
    public enum FinanceExportPostingStatusMode : short
    {
        PostedOnly = 0,
        PostedAndReversed = 1
    }

    /// <summary>
    /// Represents one export attempt result.
    /// </summary>
    public enum FinanceExportAttemptStatus : short
    {
        Started = 0,
        Succeeded = 1,
        Failed = 2
    }

    /// <summary>
    /// Represents the lifecycle state of a supplier invoice before payable posting.
    /// </summary>
    public enum SupplierInvoiceStatus : short
    {
        Draft = 0,
        Matched = 1,
        Approved = 2,
        Voided = 3,
        Posted = 4
    }

    /// <summary>
    /// Represents the purchase/receipt matching state of a supplier invoice line.
    /// </summary>
    public enum SupplierInvoiceLineMatchStatus : short
    {
        Unmatched = 0,
        Matched = 1,
        Discrepancy = 2
    }
}
