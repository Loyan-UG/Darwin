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
        SupplierInvoicePosted = 8,
        SupplierPaymentPosted = 9,
        SupplierPaymentBankSettled = 10,
        SupplierPaymentBankCorrection = 11,
        SupplierAdvancePosted = 12,
        SupplierAdvanceApplied = 13,
        PayrollRunPosted = 14,
        PayrollPaymentPosted = 15,
        PayrollPaymentBankSettled = 16,
        PayrollPaymentBankCorrection = 17
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
        TaxReceivable = 9,
        SupplierAdvance = 10,
        PayrollExpense = 11,
        EmployerPayrollTaxExpense = 12,
        PayrollPayable = 13,
        PayrollTaxPayable = 14,
        SocialInsurancePayable = 15
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

    /// <summary>
    /// Represents the lifecycle state of a formal supplier payment.
    /// </summary>
    public enum SupplierPaymentStatus : short
    {
        Draft = 0,
        Posted = 1,
        Cancelled = 2,
        Reversed = 3
    }

    /// <summary>
    /// Represents the operator-entered settlement method for a supplier payment.
    /// </summary>
    public enum SupplierPaymentMethod : short
    {
        BankTransfer = 0,
        Cash = 1,
        Card = 2,
        DirectDebit = 3,
        Other = 4
    }

    public enum BankAccountStatus : short
    {
        Active = 0,
        Archived = 1
    }

    public enum BankStatementImportStatus : short
    {
        Imported = 0,
        Cancelled = 1
    }

    public enum BankStatementLineDirection : short
    {
        Debit = 0,
        Credit = 1
    }

    public enum BankStatementLineReviewStatus : short
    {
        Unreviewed = 0,
        Reviewed = 1,
        Ignored = 2
    }

    public enum BankReconciliationMatchStatus : short
    {
        Draft = 0,
        Matched = 1,
        Cancelled = 2
    }

    public enum BankReconciliationSourceType : short
    {
        JournalEntry = 0,
        SupplierPayment = 1,
        CustomerPayment = 2,
        Refund = 3
    }

    public enum SupplierPaymentBankCorrectionType : short
    {
        ReturnedTransfer = 0,
        DuplicatePayment = 1
    }

    public enum SupplierPaymentBankCorrectionStatus : short
    {
        Draft = 0,
        Posted = 1,
        Cancelled = 2
    }

    public enum SupplierAdvanceStatus : short
    {
        Draft = 0,
        Posted = 1,
        Applied = 2,
        Cancelled = 3,
        Reversed = 4
    }
}
