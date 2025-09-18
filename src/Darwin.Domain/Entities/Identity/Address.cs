using System;
using Darwin.Domain.Common;


namespace Darwin.Domain.Entities.Identity
{
    /// <summary>
    /// Address book entry reusable across orders. Orders also snapshot addresses in JSON.
    /// </summary>
    public sealed class Address : BaseEntity
    {
        /// <summary>Owner user id; null when used as a shared address.</summary>
        public Guid? UserId { get; set; }
        /// <summary>Contact person full name.</summary>
        public string FullName { get; set; } = string.Empty;
        /// <summary>Company name if applicable.</summary>
        public string? Company { get; set; }
        /// <summary>Street line 1.</summary>
        public string Street1 { get; set; } = string.Empty;
        /// <summary>Street line 2 (optional).</summary>
        public string? Street2 { get; set; }
        /// <summary>Postal code.</summary>
        public string PostalCode { get; set; } = string.Empty;
        /// <summary>City or locality.</summary>
        public string City { get; set; } = string.Empty;
        /// <summary>State or province (nullable for countries without states).</summary>
        public string? State { get; set; }
        /// <summary>ISO 3166-1 alpha-2 country code, e.g., "DE".</summary>
        public string CountryCode { get; set; } = "DE";
        /// <summary>Phone number in E.164 format.</summary>
        public string? PhoneE164 { get; set; }
        /// <summary>VAT id used for B2B billing, if any.</summary>
        public string? VatId { get; set; }
        /// <summary>Whether this address is the default billing address for the user.</summary>
        public bool IsDefaultBilling { get; set; }
        /// <summary>Whether this address is the default shipping address for the user.</summary>
        public bool IsDefaultShipping { get; set; }
    }
}