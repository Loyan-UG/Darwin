using System;

namespace Darwin.Application.Identity.DTOs
{
    /// <summary>
    /// Lightweight projection for listing a user's addresses on edit screens.
    /// </summary>
    public sealed class AddressListItemDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string FullName { get; set; } = string.Empty;
        public string? Company { get; set; }
        public string Street1 { get; set; } = string.Empty;
        public string? Street2 { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? State { get; set; }
        public string CountryCode { get; set; } = "DE";
        public string? PhoneE164 { get; set; }
        public bool IsDefaultBilling { get; set; }
        public bool IsDefaultShipping { get; set; }
    }

    /// <summary>
    /// DTO for creating a new address for a user.
    /// </summary>
    public sealed class AddressCreateDto
    {
        public Guid UserId { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string? Company { get; set; }
        public string Street1 { get; set; } = string.Empty;
        public string? Street2 { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? State { get; set; }
        public string CountryCode { get; set; } = "DE";
        public string? PhoneE164 { get; set; }

        /// <summary>
        /// When true, this newly created address will be set as the default billing address for the user.
        /// </summary>
        public bool IsDefaultBilling { get; set; }

        /// <summary>
        /// When true, this newly created address will be set as the default shipping address for the user.
        /// </summary>
        public bool IsDefaultShipping { get; set; }
    }

    /// <summary>
    /// DTO for editing an existing address.
    /// </summary>
    public sealed class AddressEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public string FullName { get; set; } = string.Empty;
        public string? Company { get; set; }
        public string Street1 { get; set; } = string.Empty;
        public string? Street2 { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? State { get; set; }
        public string CountryCode { get; set; } = "DE";
        public string? PhoneE164 { get; set; }
        public bool IsDefaultBilling { get; set; }
        public bool IsDefaultShipping { get; set; }
    }

    /// <summary>
    /// DTO for soft-deleting an address.
    /// </summary>
    public sealed class AddressDeleteDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Composite DTO returned for admin edit screens, combining user edit data and addresses.
    /// </summary>
    public sealed class UserWithAddressesEditDto
    {
        // User edit portion (aligned with existing UserEditDto semantics)
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Locale { get; set; } = "de-DE";
        public string Timezone { get; set; } = "Europe/Berlin";
        public string Currency { get; set; } = "EUR";
        public string? PhoneE164 { get; set; }
        public bool IsActive { get; set; }

        // Addresses
        public AddressListItemDto[] Addresses { get; set; } = Array.Empty<AddressListItemDto>();
    }
}
