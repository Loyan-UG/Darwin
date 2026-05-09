using System;
using System.Collections.Generic;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.CRM
{
    /// <summary>
    /// Represents a CRM customer profile that may or may not be linked to a registered identity user.
    /// When <see cref="UserId"/> is set, contact and default address data should be resolved from the
    /// linked identity aggregate instead of duplicating profile state in CRM.
    /// </summary>
    public sealed class Customer : BaseEntity
    {
        /// <summary>
        /// Gets or sets the optional registered user id linked to this customer.
        /// When set, UI and application services should treat the identity user as the primary source
        /// for profile and contact details.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the fallback first name used only for lead-style or guest customers
        /// that do not yet have a linked identity user.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the fallback last name used only for customers without <see cref="UserId"/>.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the fallback email address used only when the customer is not linked
        /// to a registered identity user.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the fallback phone number used only when the customer is not linked
        /// to a registered identity user.
        /// </summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional company name for B2B CRM scenarios.
        /// </summary>
        public string? CompanyName { get; set; }

        /// <summary>
        /// Gets or sets whether the customer should be treated as B2C or B2B for billing support.
        /// </summary>
        public CustomerTaxProfileType TaxProfileType { get; set; } = CustomerTaxProfileType.Consumer;

        /// <summary>
        /// Gets or sets the optional VAT / tax id for business customers.
        /// </summary>
        public string? VatId { get; set; }

        /// <summary>
        /// Gets or sets the latest VAT validation status for business tax compliance review.
        /// </summary>
        public CustomerVatValidationStatus VatValidationStatus { get; set; } = CustomerVatValidationStatus.Unknown;

        /// <summary>
        /// Gets or sets when the VAT validation status was last reviewed.
        /// </summary>
        public DateTime? VatValidationCheckedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the operator or provider source that produced the VAT validation status.
        /// </summary>
        public string? VatValidationSource { get; set; }

        /// <summary>
        /// Gets or sets an optional operator-facing VAT validation note.
        /// </summary>
        public string? VatValidationMessage { get; set; }

        /// <summary>
        /// Gets or sets internal notes visible to operators.
        /// Do not store credentials, payment secrets, or regulated personal data beyond approved policy.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets segment memberships for CRM segmentation and targeting.
        /// </summary>
        public List<CustomerSegmentMembership> CustomerSegments { get; set; } = new();

        /// <summary>
        /// Gets or sets CRM-managed addresses for customers that do not rely on identity-owned addresses.
        /// </summary>
        public List<CustomerAddress> Addresses { get; set; } = new();

        /// <summary>
        /// Gets or sets interactions related directly to this customer.
        /// </summary>
        public List<Interaction> Interactions { get; set; } = new();

        /// <summary>
        /// Gets or sets consent records related to this customer.
        /// </summary>
        public List<Consent> Consents { get; set; } = new();

        /// <summary>
        /// Gets or sets sales opportunities tracked for this customer.
        /// </summary>
        public List<Opportunity> Opportunities { get; set; } = new();

        /// <summary>
        /// Gets or sets invoices linked to this customer when CRM-specific invoicing is used.
        /// </summary>
        public List<Invoice> Invoices { get; set; } = new();
    }

    /// <summary>
    /// Represents an address record owned by the CRM customer aggregate.
    /// This entity is primarily intended for guests, leads, or customers without a linked identity account.
    /// When <see cref="AddressId"/> is set, the CRM address may reference a normalized identity address record.
    /// </summary>
    public sealed class CustomerAddress : BaseEntity
    {
        /// <summary>
        /// Gets or sets the owning customer id.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the optional linked identity address id.
        /// </summary>
        public Guid? AddressId { get; set; }

        /// <summary>
        /// Gets or sets the first address line.
        /// </summary>
        public string Line1 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional second address line.
        /// </summary>
        public string? Line2 { get; set; }

        /// <summary>
        /// Gets or sets the city or locality.
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional state or province.
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Gets or sets the postal or ZIP code.
        /// </summary>
        public string PostalCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ISO country code or country identifier used by the CRM workflow.
        /// </summary>
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this is the default shipping address
        /// when CRM-owned addresses are in use.
        /// </summary>
        public bool IsDefaultShipping { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the default billing address
        /// when CRM-owned addresses are in use.
        /// </summary>
        public bool IsDefaultBilling { get; set; }
    }

    /// <summary>
    /// Defines a reusable CRM customer segment.
    /// </summary>
    public sealed class CustomerSegment : BaseEntity
    {
        /// <summary>
        /// Gets or sets the segment name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional internal description of the segment.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets memberships that attach customers to this segment.
        /// </summary>
        public List<CustomerSegmentMembership> Memberships { get; set; } = new();
    }

    /// <summary>
    /// Represents a membership link between a customer and a segment.
    /// </summary>
    public sealed class CustomerSegmentMembership : BaseEntity
    {
        /// <summary>
        /// Gets or sets the customer id.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the segment id.
        /// </summary>
        public Guid CustomerSegmentId { get; set; }
    }

    /// <summary>
    /// Stores an interaction, note, email, call, or other timeline event associated with CRM records.
    /// Interactions may target a customer, lead, or opportunity depending on the workflow stage.
    /// </summary>
    public sealed class Interaction : BaseEntity
    {
        /// <summary>
        /// Gets or sets the optional customer id related to this interaction.
        /// </summary>
        public Guid? CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the optional lead id related to this interaction.
        /// </summary>
        public Guid? LeadId { get; set; }

        /// <summary>
        /// Gets or sets the optional opportunity id related to this interaction.
        /// </summary>
        public Guid? OpportunityId { get; set; }

        /// <summary>
        /// Gets or sets the interaction type.
        /// </summary>
        public InteractionType Type { get; set; } = InteractionType.Email;

        /// <summary>
        /// Gets or sets the optional subject or title.
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets the interaction content or note body.
        /// Avoid storing secrets, payment credentials, or high-risk regulated data.
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Gets or sets the interaction channel.
        /// </summary>
        public InteractionChannel Channel { get; set; } = InteractionChannel.Email;

        /// <summary>
        /// Gets or sets the optional operator or staff user id responsible for the interaction.
        /// </summary>
        public Guid? UserId { get; set; }
    }

    /// <summary>
    /// Stores a consent decision for a customer to support privacy, marketing, and compliance workflows.
    /// </summary>
    public sealed class Consent : BaseEntity
    {
        /// <summary>
        /// Gets or sets the customer id associated with the consent record.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the consent type.
        /// </summary>
        public ConsentType Type { get; set; } = ConsentType.MarketingEmail;

        /// <summary>
        /// Gets or sets a value indicating whether consent was granted.
        /// </summary>
        public bool Granted { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when consent was granted.
        /// </summary>
        public DateTime GrantedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when consent was revoked, if applicable.
        /// </summary>
        public DateTime? RevokedAtUtc { get; set; }
    }

    /// <summary>
    /// Represents a CRM lead before conversion into a full customer relationship.
    /// </summary>
    public sealed class Lead : BaseEntity
    {
        /// <summary>
        /// Gets or sets the lead first name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the lead last name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional company name.
        /// </summary>
        public string? CompanyName { get; set; }

        /// <summary>
        /// Gets or sets the primary email address for the lead.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the primary phone number for the lead.
        /// </summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional acquisition source such as referral, ad campaign, or event.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets optional internal notes for qualification or follow-up.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets the current lead status.
        /// </summary>
        public LeadStatus Status { get; set; } = LeadStatus.New;

        /// <summary>
        /// Gets or sets the optional responsible user id for the lead.
        /// </summary>
        public Guid? AssignedToUserId { get; set; }

        /// <summary>
        /// Gets or sets the optional customer id once the lead has been converted.
        /// </summary>
        public Guid? CustomerId { get; set; }

        /// <summary>
        /// Gets or sets interactions recorded against the lead.
        /// </summary>
        public List<Interaction> Interactions { get; set; } = new();
    }

    /// <summary>
    /// Represents a revenue opportunity linked to a customer.
    /// </summary>
    public sealed class Opportunity : BaseEntity
    {
        /// <summary>
        /// Gets or sets the related customer id.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the opportunity title or short summary.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the estimated monetary value in minor currency units.
        /// </summary>
        public long EstimatedValueMinor { get; set; }

        /// <summary>
        /// Gets or sets the current opportunity stage.
        /// </summary>
        public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

        /// <summary>
        /// Gets or sets the expected close date in UTC, if known.
        /// </summary>
        public DateTime? ExpectedCloseDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the optional owner or responsible user id.
        /// </summary>
        public Guid? AssignedToUserId { get; set; }

        /// <summary>
        /// Gets or sets line items being discussed or quoted for the opportunity.
        /// </summary>
        public List<OpportunityItem> Items { get; set; } = new();

        /// <summary>
        /// Gets or sets interactions linked to the opportunity.
        /// </summary>
        public List<Interaction> Interactions { get; set; } = new();
    }

    /// <summary>
    /// Represents a product line under an opportunity.
    /// </summary>
    public sealed class OpportunityItem : BaseEntity
    {
        /// <summary>
        /// Gets or sets the owning opportunity id.
        /// </summary>
        public Guid OpportunityId { get; set; }

        /// <summary>
        /// Gets or sets the product variant id being discussed.
        /// </summary>
        public Guid ProductVariantId { get; set; }

        /// <summary>
        /// Gets or sets the requested quantity.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the quoted unit price in minor currency units.
        /// </summary>
        public long UnitPriceMinor { get; set; }
    }

    /// <summary>
    /// Represents an invoice that may belong to an order flow, a CRM flow, or both.
    /// This entity intentionally supports order and CRM scenarios to avoid parallel invoice models.
    /// </summary>
    public sealed class Invoice : BaseEntity
    {
        /// <summary>
        /// Gets or sets the optional business id when the invoice is scoped to a business tenant.
        /// </summary>
        public Guid? BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the optional customer id for CRM-driven invoices.
        /// </summary>
        public Guid? CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the optional order id for order-driven invoices.
        /// </summary>
        public Guid? OrderId { get; set; }

        /// <summary>
        /// Gets or sets the optional payment id that settles the invoice.
        /// </summary>
        public Guid? PaymentId { get; set; }

        /// <summary>
        /// Gets or sets the invoice lifecycle status.
        /// </summary>
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        /// <summary>
        /// Gets or sets the ISO 4217 currency code.
        /// </summary>
        public string Currency { get; set; } = DomainDefaults.DefaultCurrency;

        /// <summary>
        /// Gets or sets the total net amount in minor units.
        /// </summary>
        public long TotalNetMinor { get; set; }

        /// <summary>
        /// Gets or sets the total tax amount in minor units.
        /// </summary>
        public long TotalTaxMinor { get; set; }

        /// <summary>
        /// Gets or sets the total gross amount in minor units.
        /// </summary>
        public long TotalGrossMinor { get; set; }

        /// <summary>
        /// Gets or sets the invoice due date in UTC.
        /// </summary>
        public DateTime DueDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the invoice was paid.
        /// </summary>
        public DateTime? PaidAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the invoice was first issued.
        /// </summary>
        public DateTime? IssuedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the final operator reverse-charge decision for this invoice.
        /// </summary>
        public bool? ReverseChargeApplied { get; set; }

        /// <summary>
        /// Gets or sets when the reverse-charge decision was reviewed.
        /// </summary>
        public DateTime? ReverseChargeReviewedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets an optional operator note for the reverse-charge decision.
        /// </summary>
        public string? ReverseChargeReviewNote { get; set; }

        /// <summary>
        /// Gets or sets the immutable JSON invoice snapshot captured when the invoice is issued.
        /// </summary>
        public string? IssuedSnapshotJson { get; set; }

        /// <summary>
        /// Gets or sets the SHA-256 hash of the immutable issued invoice snapshot.
        /// </summary>
        public string? IssuedSnapshotHashSha256 { get; set; }

        /// <summary>
        /// Gets or sets when the archive metadata was generated from the issued snapshot.
        /// </summary>
        public DateTime? ArchiveGeneratedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets when the issued invoice archive may be eligible for retention cleanup.
        /// </summary>
        public DateTime? ArchiveRetainUntilUtc { get; set; }

        /// <summary>
        /// Gets or sets the retention policy version used when archive metadata was generated.
        /// </summary>
        public string? ArchiveRetentionPolicyVersion { get; set; }

        /// <summary>
        /// Gets or sets when the issued invoice archive payload was purged after the retention period.
        /// </summary>
        public DateTime? ArchivePurgedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the reason recorded for archive payload purge.
        /// </summary>
        public string? ArchivePurgeReason { get; set; }

        /// <summary>
        /// Gets or sets invoice lines.
        /// </summary>
        public List<InvoiceLine> Lines { get; set; } = new();
    }

    /// <summary>
    /// Represents a single invoice line item.
    /// </summary>
    public sealed class InvoiceLine : BaseEntity
    {
        /// <summary>
        /// Gets or sets the owning invoice id.
        /// </summary>
        public Guid InvoiceId { get; set; }

        /// <summary>
        /// Gets or sets the line description displayed to the user or operator.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the unit price excluding tax in minor units.
        /// </summary>
        public long UnitPriceNetMinor { get; set; }

        /// <summary>
        /// Gets or sets the tax rate as a decimal fraction such as 0.19 for 19%.
        /// </summary>
        public decimal TaxRate { get; set; }

        /// <summary>
        /// Gets or sets the total net amount in minor units.
        /// </summary>
        public long TotalNetMinor { get; set; }

        /// <summary>
        /// Gets or sets the total gross amount in minor units.
        /// </summary>
        public long TotalGrossMinor { get; set; }
    }
}
