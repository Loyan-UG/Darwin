using Darwin.Domain.Entities.CRM;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Persistence.Db
{
    /// <summary>
    /// CRM-focused DbSets.
    /// </summary>
    public sealed partial class DarwinDbContext
    {
        /// <summary>
        /// CRM customer profiles.
        /// </summary>
        public DbSet<Customer> Customers => Set<Customer>();

        /// <summary>
        /// CRM-managed customer addresses.
        /// </summary>
        public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();

        /// <summary>
        /// CRM customer segment definitions.
        /// </summary>
        public DbSet<CustomerSegment> CustomerSegments => Set<CustomerSegment>();

        /// <summary>
        /// CRM customer segment memberships.
        /// </summary>
        public DbSet<CustomerSegmentMembership> CustomerSegmentMemberships => Set<CustomerSegmentMembership>();

        /// <summary>
        /// CRM interactions and notes.
        /// </summary>
        public DbSet<Interaction> Interactions => Set<Interaction>();

        /// <summary>
        /// CRM consent records.
        /// </summary>
        public DbSet<Consent> Consents => Set<Consent>();

        /// <summary>
        /// CRM leads.
        /// </summary>
        public DbSet<Lead> Leads => Set<Lead>();

        /// <summary>
        /// CRM sales opportunities.
        /// </summary>
        public DbSet<Opportunity> Opportunities => Set<Opportunity>();

        /// <summary>
        /// CRM opportunity line items.
        /// </summary>
        public DbSet<OpportunityItem> OpportunityItems => Set<OpportunityItem>();

        /// <summary>
        /// CRM and order-capable invoices.
        /// </summary>
        public DbSet<Invoice> Invoices => Set<Invoice>();

        /// <summary>
        /// Invoice line items.
        /// </summary>
        public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    }
}
