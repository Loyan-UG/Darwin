using System;
using System.Collections.Generic;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Integration;
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
        /// IANA time zone identifier used for business-facing scheduling, timestamps, and operational messaging.
        /// This remains business-scoped even when the hosting platform itself runs in a different server time zone.
        /// </summary>
        public string DefaultTimeZoneId { get; set; } = "Europe/Berlin";

        /// <summary>
        /// Optional JSON map of business-scoped admin text overrides layered on top of platform shared resources.
        /// Format: { "de-DE": { "SomeKey": "..." }, "en-US": { "SomeKey": "..." } }.
        /// Intended for tenant-specific operator wording where one business needs different terminology than the platform baseline.
        /// </summary>
        public string? AdminTextOverridesJson { get; set; }

        /// <summary>
        /// Optional display name used for branded customer-facing content when it should differ from the internal business name.
        /// This is useful for storefront, email templates, and receipts without changing the legal or operational business names.
        /// </summary>
        public string? BrandDisplayName { get; set; }

        /// <summary>
        /// Optional logo URL used for business-branded content such as emails, hosted pages, or future storefront widgets.
        /// The value should be an absolute or application-relative URL and must not point to private internal storage paths.
        /// </summary>
        public string? BrandLogoUrl { get; set; }

        /// <summary>
        /// Optional primary brand color in hex format (for example, "#0055AA").
        /// This is intended for lightweight branding and should remain a presentational hint, not a security-sensitive field.
        /// </summary>
        public string? BrandPrimaryColorHex { get; set; }

        /// <summary>
        /// Optional secondary or accent brand color in hex format.
        /// This complements <see cref="BrandPrimaryColorHex"/> for future storefront and communication theming.
        /// </summary>
        public string? BrandSecondaryColorHex { get; set; }

        /// <summary>
        /// Optional email address used for customer support or outbound communication reply handling.
        /// This may differ from the public contact email and should be suitable for operational support traffic.
        /// </summary>
        public string? SupportEmail { get; set; }

        /// <summary>
        /// Optional display name used as the sender name in business-scoped email communication.
        /// This is a business-level communication preference and not a transport-level SMTP setting.
        /// </summary>
        public string? CommunicationSenderName { get; set; }

        /// <summary>
        /// Optional reply-to email address for business-scoped communication templates.
        /// When omitted, outbound communication should fall back to the broader support or contact email strategy.
        /// </summary>
        public string? CommunicationReplyToEmail { get; set; }

        /// <summary>
        /// Indicates whether transactional customer emails are enabled for this business.
        /// This is a business-level preference and does not replace provider-level transport configuration.
        /// </summary>
        public bool CustomerEmailNotificationsEnabled { get; set; } = true;

        /// <summary>
        /// Indicates whether customer-facing marketing emails are allowed for this business.
        /// Consent and audience eligibility rules still apply separately.
        /// </summary>
        public bool CustomerMarketingEmailsEnabled { get; set; }

        /// <summary>
        /// Indicates whether operational alert emails should be sent to business-facing recipients.
        /// Examples include onboarding warnings, payment exceptions, and shipping issues.
        /// </summary>
        public bool OperationalAlertEmailsEnabled { get; set; } = true;

        /// <summary>
        /// Whether the business is active on the platform. Inactive businesses are hidden from discovery and blocked from scanning.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Operational lifecycle state used by onboarding, approval, and suspension workflows.
        /// This value helps WebAdmin and support tooling distinguish "not approved yet" from "suspended after go-live".
        /// </summary>
        public BusinessOperationalStatus OperationalStatus { get; set; } = BusinessOperationalStatus.PendingApproval;

        /// <summary>
        /// Timestamp of the latest approval decision. This is null until the business is approved at least once.
        /// </summary>
        public DateTime? ApprovedAtUtc { get; set; }

        /// <summary>
        /// Timestamp of the latest suspension action. This is cleared when the business is reactivated.
        /// </summary>
        public DateTime? SuspendedAtUtc { get; set; }

        /// <summary>
        /// Optional operator-entered note that explains why the business was suspended.
        /// This should remain concise and must not contain secrets or unnecessary PII.
        /// </summary>
        public string? SuspensionReason { get; set; }

        /// <summary>
        /// Navigation: Members (users with roles) belonging to this business.
        /// </summary>
        public ICollection<BusinessMember> Members { get; } = new List<BusinessMember>();

        /// <summary>
        /// Navigation: Physical or virtual locations owned by this business.
        /// </summary>
        public ICollection<BusinessLocation> Locations { get; } = new List<BusinessLocation>();

        /// <summary>
        /// Navigation: Favorites created by users for this business (discovery feature).
        /// </summary>
        public ICollection<BusinessFavorite> Favorites { get; } = new List<BusinessFavorite>();

        /// <summary>
        /// Navigation: Likes created by users for this business (discovery feature).
        /// </summary>
        public ICollection<BusinessLike> Likes { get; } = new List<BusinessLike>();

        /// <summary>
        /// Navigation: Reviews created by users for this business (discovery feature).
        /// </summary>
        public ICollection<BusinessReview> Reviews { get; } = new List<BusinessReview>();

        /// <summary>
        /// Navigation: Cached engagement stats for this business (optional 1:1 row).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is intentionally modeled as an optional 1:1 navigation (not a collection).
        /// A single row per business enables fast reads for discovery pages and avoids duplicate aggregates.
        /// </para>
        /// <para>
        /// Infrastructure must enforce uniqueness on <see cref="BusinessEngagementStats.BusinessId"/>.
        /// </para>
        /// </remarks>
        public BusinessEngagementStats? EngagementStats { get; private set; }


        /// <summary>
        /// Navigation: Invitations issued for onboarding into this business.
        /// </summary>
        public ICollection<BusinessInvitation> Invitations { get; } = new List<BusinessInvitation>();

        /// <summary>
        /// Navigation: Staff QR codes issued within this business.
        /// </summary>
        public ICollection<BusinessStaffQrCode> StaffQrCodes { get; } = new List<BusinessStaffQrCode>();

        /// <summary>
        /// Navigation: Business subscription records (billing history).
        /// </summary>
        public ICollection<BusinessSubscription> Subscriptions { get; } = new List<BusinessSubscription>();

        /// <summary>
        /// Navigation: Analytics export jobs scoped to this business.
        /// </summary>
        public ICollection<AnalyticsExportJob> AnalyticsExportJobs { get; } = new List<AnalyticsExportJob>();
    }
}
