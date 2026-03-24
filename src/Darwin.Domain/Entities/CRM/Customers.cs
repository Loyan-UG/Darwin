using System;
using System.Collections.Generic;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.CRM
{
    /// <summary>
    /// CRM customer profile. A customer can exist with or without an identity user account.
    /// </summary>
    public sealed class Customer : BaseEntity
    {
        /// <summary>Customer first name.</summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>Customer last name.</summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>Primary email address.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Primary phone number.</summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>Optional company name for B2B cases.</summary>
        public string? CompanyName { get; set; }

        /// <summary>Optional internal notes.</summary>
        public string? Notes { get; set; }

        /// <summary>Aggregated loyalty points balance.</summary>
        public int LoyaltyPointsTotal { get; set; }

        /// <summary>Customer segment memberships.</summary>
        public List<CustomerSegmentMembership> CustomerSegments { get; set; } = new();

        /// <summary>Customer addresses.</summary>
        public List<CustomerAddress> Addresses { get; set; } = new();

        /// <summary>Customer interactions timeline.</summary>
        public List<Interaction> Interactions { get; set; } = new();

        /// <summary>Customer consent history.</summary>
        public List<Consent> Consents { get; set; } = new();

        /// <summary>Customer loyalty point entries.</summary>
        public List<LoyaltyPointEntry> LoyaltyPointEntries { get; set; } = new();

        /// <summary>Customer invoices.</summary>
        public List<Invoice> Invoices { get; set; } = new();
    }

    /// <summary>
    /// Customer address entry.
    /// </summary>
    public sealed class CustomerAddress : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public string Line1 { get; set; } = string.Empty;
        public string? Line2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string? State { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public bool IsDefaultShipping { get; set; }
        public bool IsDefaultBilling { get; set; }
    }

    /// <summary>
    /// Customer segment definition.
    /// </summary>
    public sealed class CustomerSegment : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<CustomerSegmentMembership> Memberships { get; set; } = new();
    }

    /// <summary>
    /// Membership join entity between customer and segment.
    /// </summary>
    public sealed class CustomerSegmentMembership : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public Guid CustomerSegmentId { get; set; }
    }

    /// <summary>
    /// CRM interaction event.
    /// </summary>
    public sealed class Interaction : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public InteractionType Type { get; set; } = InteractionType.Email;
        public string? Subject { get; set; }
        public string? Content { get; set; }
        public InteractionChannel Channel { get; set; } = InteractionChannel.Email;
        public Guid? UserId { get; set; }
    }

    /// <summary>
    /// Customer consent record for GDPR compliance.
    /// </summary>
    public sealed class Consent : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public ConsentType Type { get; set; } = ConsentType.MarketingEmail;
        public bool Granted { get; set; }
        public DateTime GrantedAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
    }

    /// <summary>
    /// Customer loyalty points ledger entry.
    /// </summary>
    public sealed class LoyaltyPointEntry : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public int Points { get; set; }
        public string Reason { get; set; } = string.Empty;
        public Guid? ReferenceId { get; set; }
    }

    /// <summary>
    /// CRM invoice aggregate.
    /// </summary>
    public sealed class Invoice : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public Guid? OrderId { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
        public string Currency { get; set; } = "EUR";
        public long TotalNetMinor { get; set; }
        public long TotalTaxMinor { get; set; }
        public long TotalGrossMinor { get; set; }
        public DateTime DueDateUtc { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public List<InvoiceLine> Lines { get; set; } = new();
    }

    /// <summary>
    /// Line item for CRM invoice.
    /// </summary>
    public sealed class InvoiceLine : BaseEntity
    {
        public Guid InvoiceId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public long UnitPriceNetMinor { get; set; }
        public decimal TaxRate { get; set; }
        public long TotalNetMinor { get; set; }
        public long TotalGrossMinor { get; set; }
    }
}
