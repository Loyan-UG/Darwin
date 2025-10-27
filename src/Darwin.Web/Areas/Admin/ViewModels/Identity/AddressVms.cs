using System;
using System.Collections.Generic;

namespace Darwin.Web.Areas.Admin.ViewModels.Identity
{
    /// <summary>
    /// View model used inside the User edit screen to show and manage the user's addresses.
    /// This model is not intended for a global "all addresses" page.
    /// </summary>
    public sealed class UserAddressesSectionVm
    {
        /// <summary>
        /// The owner user id for which these addresses are being managed.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Current non-deleted addresses of the user, listed in a compact grid on the User edit page.
        /// </summary>
        public List<UserAddressListItemVm> Items { get; set; } = new();
    }


    /// <summary>
    /// Lightweight row shown in the addresses table embedded in the User edit page.
    /// All field names mirror the Domain entity to avoid drift.
    /// </summary>
    public sealed class UserAddressListItemVm
    {
        /// <summary>Primary key of the address row.</summary>
        public Guid Id { get; set; }

        /// <summary>Optimistic concurrency token. Must be posted back on edits/deletes.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        /// <summary>Contact person full name.</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>Optional company name.</summary>
        public string? Company { get; set; }

        /// <summary>Street line 1 (required).</summary>
        public string Street1 { get; set; } = string.Empty;

        /// <summary>Street line 2 (optional).</summary>
        public string? Street2 { get; set; }

        /// <summary>Postal/ZIP code (required).</summary>
        public string PostalCode { get; set; } = string.Empty;

        /// <summary>City or locality (required).</summary>
        public string City { get; set; } = string.Empty;

        /// <summary>State or province (optional; depends on country).</summary>
        public string? State { get; set; }

        /// <summary>ISO 3166-1 alpha-2 country code (required), e.g., "DE".</summary>
        public string CountryCode { get; set; } = "DE";

        /// <summary>Phone number in E.164 format (optional).</summary>
        public string? PhoneE164 { get; set; }


        /// <summary>Whether this row is the default billing address for the user.</summary>
        public bool IsDefaultBilling { get; set; }

        /// <summary>Whether this row is the default shipping address for the user.</summary>
        public bool IsDefaultShipping { get; set; }
    }


    /// <summary>
    /// Form model for creating a new address for the user from the User edit page.
    /// Validation rules are enforced in Application; comments here guide the UI.
    /// </summary>
    public sealed class UserAddressCreateVm
    {
        /// <summary>
        /// Owner user id. In Admin this is required and comes from the surrounding User edit page.
        /// </summary>
        public Guid UserId { get; set; }

        public string FullName { get; set; } = string.Empty;          // Max length 200 (Application validates)
        public string? Company { get; set; }                           // Max length 200
        public string Street1 { get; set; } = string.Empty;            // Required, Max length 300
        public string? Street2 { get; set; }                           // Max length 300
        public string PostalCode { get; set; } = string.Empty;         // Required, Max length 32
        public string City { get; set; } = string.Empty;               // Required, Max length 150
        public string? State { get; set; }                             // Max length 150
        public string CountryCode { get; set; } = "DE";                // Required, Length 2
        public string? PhoneE164 { get; set; }                         // Max length 20

        /// <summary>
        /// When checked, the handler must ensure this becomes the only default billing address for the user.
        /// </summary>
        public bool IsDefaultBilling { get; set; }

        /// <summary>
        /// When checked, the handler must ensure this becomes the only default shipping address for the user.
        /// </summary>
        public bool IsDefaultShipping { get; set; }
    }

    /// <summary>
    /// Form model for editing an existing address from the User edit page. Includes concurrency control.
    /// </summary>
    public sealed class UserAddressEditVm
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        /// <summary>
        /// Concurrency token mirrored from the entity RowVersion. Required on POST.
        /// </summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public string FullName { get; set; } = string.Empty;          // Max length 200
        public string? Company { get; set; }                           // Max length 200
        public string Street1 { get; set; } = string.Empty;            // Required, Max length 300
        public string? Street2 { get; set; }                           // Max length 300
        public string PostalCode { get; set; } = string.Empty;         // Required, Max length 32
        public string City { get; set; } = string.Empty;               // Required, Max length 150
        public string? State { get; set; }                             // Max length 150
        public string CountryCode { get; set; } = "DE";                // Required, Length 2
        public string? PhoneE164 { get; set; }                         // Max length 20
        
        public bool IsDefaultBilling { get; set; }
        public bool IsDefaultShipping { get; set; }
    }
}
