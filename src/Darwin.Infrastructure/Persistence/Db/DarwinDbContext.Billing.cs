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
