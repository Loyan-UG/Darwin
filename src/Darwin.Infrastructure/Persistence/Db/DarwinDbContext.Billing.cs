using Darwin.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Persistence.Db
{
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
        /// Provider-synchronized invoices for <see cref="BusinessSubscription"/>.
        /// </summary>
        public DbSet<SubscriptionInvoice> SubscriptionInvoices => Set<SubscriptionInvoice>();
    }
}   
