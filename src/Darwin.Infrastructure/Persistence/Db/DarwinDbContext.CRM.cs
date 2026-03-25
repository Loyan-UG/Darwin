using Darwin.Domain.Entities.CRM;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Persistence.Db
{
    /// <summary>
    /// CRM DbSets.
    /// </summary>
    public sealed partial class DarwinDbContext
    {
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
        public DbSet<CustomerSegment> CustomerSegments => Set<CustomerSegment>();
        public DbSet<CustomerSegmentMembership> CustomerSegmentMemberships => Set<CustomerSegmentMembership>();
        public DbSet<Interaction> Interactions => Set<Interaction>();
        public DbSet<Consent> Consents => Set<Consent>();
        public DbSet<LoyaltyPointEntry> LoyaltyPointEntries => Set<LoyaltyPointEntry>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    }
}
