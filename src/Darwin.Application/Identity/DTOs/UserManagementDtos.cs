using System;

namespace Darwin.Application.Identity.DTOs
{
    /// <summary>
    /// Request model for editing the profile of the current authenticated user (Member area).
    /// Only profile-related fields may be modified; email and activation status are immutable here.
    /// </summary>
    public sealed class UserProfileEditDto
    {
        /// <summary>The identifier of the user being updated.</summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Concurrency token used to detect conflicting updates.
        /// Should be copied from the original entity's RowVersion.
        /// </summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        /// <summary>The user's given name; may be null.</summary>
        public string? FirstName { get; set; }

        /// <summary>The user's family name; may be null.</summary>
        public string? LastName { get; set; }

        /// <summary>The preferred culture code (IETF tag), e.g. "de-DE".</summary>
        public string Locale { get; set; } = "de-DE";

        /// <summary>The preferred time zone identifier, e.g. "Europe/Berlin".</summary>
        public string Timezone { get; set; } = "Europe/Berlin";

        /// <summary>The preferred currency code (ISO 4217), e.g. "EUR".</summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>The user's phone number in E.164 format; may be null.</summary>
        public string? PhoneE164 { get; set; }
    }

    


    /// <summary>Change password input.</summary>
    public sealed class UserChangePasswordDto
    {
        public Guid Id { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
