using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Darwin.Web.Areas.Admin.ViewModels.Identity
{
    /// <summary>
    /// View model used to display a single user entry in lists.
    /// </summary>
    public sealed class UserListItemVm
    {
        /// <summary>Primary key.</summary>
        public Guid Id { get; set; }

        /// <summary>User's email address.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Given name.</summary>
        public string? FirstName { get; set; }

        /// <summary>Family name.</summary>
        public string? LastName { get; set; }

        /// <summary>Whether the account is active (enabled).</summary>
        public bool IsActive { get; set; }

        /// <summary>Whether this user is system‑protected and cannot be deleted.</summary>
        public bool IsSystem { get; set; }
    }

    /// <summary>
    /// View model used to create a new user in the admin area.
    /// </summary>
    public sealed class UserCreateVm
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>Plain text password. Must be at least 8 characters.</summary>
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        /// <summary>Given name; optional.</summary>
        public string? FirstName { get; set; }

        /// <summary>Family name; optional.</summary>
        public string? LastName { get; set; }

        /// <summary>Preferred culture code.</summary>
        [Required]
        public string Locale { get; set; } = "de-DE";

        /// <summary>Preferred time zone.</summary>
        [Required]
        public string Timezone { get; set; } = "Europe/Berlin";

        /// <summary>Preferred currency code (ISO 4217).</summary>
        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = "EUR";

        /// <summary>Phone number in E.164 format; optional.</summary>
        [Phone]
        public string? PhoneE164 { get; set; }

        /// <summary>Initial active flag; default true.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Marks the user as system‑protected; use sparingly.</summary>
        public bool IsSystem { get; set; } = false;
    }

    /// <summary>
    /// View model used to edit an existing user (admin area).
    /// Note that Email and Username are not editable here.
    /// </summary>
    public sealed class UserEditVm
    {
        public Guid Id { get; set; }

        /// <summary>Concurrency token to detect conflicting updates.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        [Required]
        public string Locale { get; set; } = "de-DE";

        [Required]
        public string Timezone { get; set; } = "Europe/Berlin";

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = "EUR";

        [Phone]
        public string? PhoneE164 { get; set; }

        /// <summary>Active flag; deactivating disables login.</summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// View model used by members to edit their own profile.
    /// Email, Username, IsActive and System flags are hidden.
    /// </summary>
    public sealed class UserProfileEditVm
    {
        public Guid Id { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        [Required]
        public string Locale { get; set; } = "de-DE";

        [Required]
        public string Timezone { get; set; } = "Europe/Berlin";

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = "EUR";

        [Phone]
        public string? PhoneE164 { get; set; }
    }

    /// <summary>
    /// View model for changing a user's email (admin).
    /// </summary>
    public sealed class UserChangeEmailVm
    {
        public Guid Id { get; set; }

        [Required]
        [EmailAddress]
        public string NewEmail { get; set; } = string.Empty;
    }

    /// <summary>
    /// View model for soft‑deleting a user. Contains only an identifier and concurrency token.
    /// </summary>
    public sealed class UserDeleteVm
    {
        public Guid Id { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// View model for changing a user's password (admin or member).
    /// </summary>
    public sealed class UserChangePasswordVm
    {
        public Guid Id { get; set; }

        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
