using System;
using System.Collections.Generic;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Businesses
{
    /// <summary>
    /// Represents a partner business (merchant) that operates on the Darwin platform.
    /// A Business can have multiple members (users with roles), multiple locations,
    /// and one or more loyalty programs. The entity is designed as a multi-tenant root
    /// for merchant-scoped data (e.g., loyalty accounts, ledgers, and on-prem operations).
    /// </summary>
    public sealed class Business : BaseEntity
    {
        /// <summary>
        /// Human-friendly display name, e.g., "Café Aurora".
        /// Use for list/detail pages and map annotations.
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Optional legal name for invoicing and compliance (e.g., "Aurora GmbH").
        /// </summary>
        public string? LegalName { get; set; }

        /// <summary>
        /// Optional tax identifier (e.g., USt-IdNr. in Germany).
        /// </summary>
        public string? TaxId { get; set; }

        /// <summary>
        /// Optional short description or business pitch for discovery.
        /// Rich formatting belongs to CMS; keep plain text here.
        /// </summary>
        public string? ShortDescription { get; set; }

        /// <summary>
        /// Optional website URL; must be absolute when provided.
        /// </summary>
        public string? WebsiteUrl { get; set; }

        /// <summary>
        /// Public contact email of the business (e.g., info@...).
        /// </summary>
        public string? ContactEmail { get; set; }

        /// <summary>
        /// Public contact phone in E.164 format (e.g., +491234567890).
        /// </summary>
        public string? ContactPhoneE164 { get; set; }

        /// <summary>
        /// High-level categorical kind for filtering and discovery.
        /// </summary>
        public BusinessCategoryKind Category { get; set; } = BusinessCategoryKind.Unknown;

        /// <summary>
        /// ISO 4217 currency code to be used by default in business operations (e.g., "EUR").
        /// </summary>
        public string DefaultCurrency { get; set; } = "EUR";

        /// <summary>
        /// Culture code preferred by the business for UI/printing (e.g., "de-DE").
        /// </summary>
        public string DefaultCulture { get; set; } = "de-DE";

        /// <summary>
        /// Whether the business is active on the platform. Inactive businesses are hidden from discovery and blocked from scanning.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Navigation: Members (users with roles) belonging to this business.
        /// </summary>
        public ICollection<BusinessMember> Members { get; } = new List<BusinessMember>();

        /// <summary>
        /// Navigation: Physical or virtual locations owned by this business.
        /// </summary>
        public ICollection<BusinessLocation> Locations { get; } = new List<BusinessLocation>();
    }
}
