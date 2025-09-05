using System;
using Darwin.Domain.Common;


namespace Darwin.Domain.Entities.Users
{
    /// <summary>
    /// Extended user profile (pairs with ASP.NET Identity user table) holding CRM-relevant fields.
    /// </summary>
    public sealed class UserProfile : BaseEntity
    {
        /// <summary>Foreign key to Identity user id (GUID assumed for alignment).</summary>
        public Guid IdentityUserId { get; set; }
        /// <summary>Given name of the user.</summary>
        public string? FirstName { get; set; }
        /// <summary>Surname of the user.</summary>
        public string? LastName { get; set; }
        /// <summary>Phone number in E.164 format.</summary>
        public string? PhoneE164 { get; set; }
        /// <summary>Company name for B2B contexts.</summary>
        public string? Company { get; set; }
        /// <summary>VAT identification number for B2B invoicing.</summary>
        public string? VatId { get; set; }


        // Defaults
        /// <summary>Default billing address id (if stored as a reusable address).</summary>
        public Guid? DefaultBillingAddressId { get; set; }
        /// <summary>Default shipping address id (if stored as a reusable address).</summary>
        public Guid? DefaultShippingAddressId { get; set; }


        // Consent & Preferences
        /// <summary>Marketing consent master switch.</summary>
        public bool MarketingConsent { get; set; }
        /// <summary>Opt-in flags for different channels serialized as JSON (e.g., {"Email":true,"SMS":false,"WhatsApp":true}).</summary>
        public string ChannelsOptInJson { get; set; } = "{}";
        /// <summary>Timestamp when terms of service were accepted by the user.</summary>
        public DateTime? AcceptsTermsAtUtc { get; set; }
        /// <summary>Preferred culture code for content and formatting.</summary>
        public string Locale { get; set; } = "de-DE";
        /// <summary>Preferred currency code for pricing.</summary>
        public string Currency { get; set; } = "EUR";
        /// <summary>Preferred time zone for displaying times.</summary>
        public string Timezone { get; set; } = "Europe/Berlin";


        // Attribution
        /// <summary>Anonymous id used prior to authentication to stitch identity.</summary>
        public string? AnonymousId { get; set; }
        /// <summary>First-touch UTM parameters serialized as JSON.</summary>
        public string FirstTouchUtmJson { get; set; } = "{}";
        /// <summary>Last-touch UTM parameters serialized as JSON.</summary>
        public string LastTouchUtmJson { get; set; } = "{}";
        /// <summary>Optional user tags for segmentation (comma-separated or JSON).</summary>
        public string? Tags { get; set; }
        /// <summary>External system identifiers serialized as JSON (e.g., PSP customer ids).</summary>
        public string ExternalIdsJson { get; set; } = "{}";
    }
}