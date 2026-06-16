using Darwin.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Persistence.Db
{
    /// <summary>
    /// Billing and lightweight accounting DbSets.
    /// </summary>
    public sealed partial class DarwinDbContext
    {
        /// <summary>
        /// Payments used by billing, orders, and CRM invoice settlement flows.
        /// </summary>
        public DbSet<Payment> Payments => Set<Payment>();

        /// <summary>
        /// Commercial subscription plans offered by the platform.
        /// </summary>
        public DbSet<BillingPlan> BillingPlans => Set<BillingPlan>();

        /// <summary>
        /// Active business subscriptions and their provider reconciliation data.
        /// </summary>
        public DbSet<BusinessSubscription> BusinessSubscriptions => Set<BusinessSubscription>();

        /// <summary>
        /// Provider-synchronized invoices for business subscriptions.
        /// </summary>
        public DbSet<SubscriptionInvoice> SubscriptionInvoices => Set<SubscriptionInvoice>();

        /// <summary>
        /// Financial accounts for lightweight bookkeeping.
        /// </summary>
        public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();

        /// <summary>
        /// Business-specific mappings from finance posting roles to financial accounts.
        /// </summary>
        public DbSet<FinancePostingAccountMapping> FinancePostingAccountMappings => Set<FinancePostingAccountMapping>();

        /// <summary>
        /// Finance export batches.
        /// </summary>
        public DbSet<FinanceExportBatch> FinanceExportBatches => Set<FinanceExportBatch>();

        /// <summary>
        /// Finance export attempts.
        /// </summary>
        public DbSet<FinanceExportAttempt> FinanceExportAttempts => Set<FinanceExportAttempt>();

        public DbSet<BankAccount> BankAccounts => Set<BankAccount>();

        public DbSet<BankStatementImport> BankStatementImports => Set<BankStatementImport>();

        public DbSet<BankStatementLine> BankStatementLines => Set<BankStatementLine>();

        public DbSet<BankReconciliationMatch> BankReconciliationMatches => Set<BankReconciliationMatch>();

        public DbSet<BankReconciliationMatchLine> BankReconciliationMatchLines => Set<BankReconciliationMatchLine>();

        /// <summary>
        /// Supplier invoices used as formal payables source documents before posting.
        /// </summary>
        public DbSet<SupplierInvoice> SupplierInvoices => Set<SupplierInvoice>();

        /// <summary>
        /// Supplier invoice line snapshots and matching state.
        /// </summary>
        public DbSet<SupplierInvoiceLine> SupplierInvoiceLines => Set<SupplierInvoiceLine>();

        /// <summary>
        /// Supplier payments used as formal payables settlement records.
        /// </summary>
        public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();

        /// <summary>
        /// Supplier payment allocations to posted supplier invoices.
        /// </summary>
        public DbSet<SupplierPaymentAllocation> SupplierPaymentAllocations => Set<SupplierPaymentAllocation>();

        /// <summary>
        /// Evidence-backed bank corrections for bank-settled supplier payments.
        /// </summary>
        public DbSet<SupplierPaymentBankCorrection> SupplierPaymentBankCorrections => Set<SupplierPaymentBankCorrection>();

        /// <summary>
        /// Journal entries.
        /// </summary>
        public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();

        /// <summary>
        /// Journal entry lines.
        /// </summary>
        public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();

        /// <summary>
        /// Recorded business expenses.
        /// </summary>
        public DbSet<Expense> Expenses => Set<Expense>();
    }
}
