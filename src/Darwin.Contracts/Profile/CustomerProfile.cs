using System;

namespace Darwin.Contracts.Profile
{
    /// <summary>
    /// Represents the editable customer profile for the currently authenticated user.
    /// This contract is used by mobile/web clients and must remain backward compatible.
    /// </summary>
    public sealed class CustomerProfile
    {
        /// <summary>
        /// The user's public identifier.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// User's email address (read-only in most scenarios; server may ignore changes).
        /// </summary>
        public string? Email { get; init; }

        /// <summary>
        /// User's first name.
        /// </summary>
        public string? FirstName { get; init; }

        /// <summary>
        /// User's last name.
        /// </summary>
        public string? LastName { get; init; }

        /// <summary>
        /// Phone number in E.164 format (e.g., +49123456789).
        /// </summary>
        public string? PhoneE164 { get; init; }

        /// <summary>
        /// Preferred UI locale/culture (e.g., "de-DE").
        /// </summary>
        public string? Locale { get; init; }

        /// <summary>
        /// Preferred IANA timezone id (e.g., "Europe/Berlin").
        /// </summary>
        public string? Timezone { get; init; }

        /// <summary>
        /// Preferred ISO 4217 currency code (e.g., "EUR").
        /// IMPORTANT: Added for contract completeness (Application requires it).
        /// </summary>
        public string? Currency { get; init; }

        /// <summary>
        /// Optimistic concurrency token (row version).
        /// Clients must round-trip this value on updates.
        /// </summary>
        public byte[]? RowVersion { get; init; }
    }
}
