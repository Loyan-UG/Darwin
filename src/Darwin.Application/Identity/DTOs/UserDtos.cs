using System;

namespace Darwin.Application.Identity.DTOs
{
    /// <summary>Light user projection for Admin grids and API.</summary>
    public sealed class UserListItemDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;         // Unique login (normalized in Infra)
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneE164 { get; set; }
        public bool IsActive { get; set; }
        public bool IsSystem { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>Create user input model.</summary>
    public sealed class UserCreateDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;      // Plain text; will be hashed in handler
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Locale { get; set; } = "de-DE";
        public string Timezone { get; set; } = "Europe/Berlin";
        public string Currency { get; set; } = "EUR";
        public string? PhoneE164 { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSystem { get; set; } = false;               // If true, protected from destructive ops
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>Edit user input model.</summary>
    public sealed class UserEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>(); // For optimistic concurrency
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Locale { get; set; } = "de-DE";
        public string Timezone { get; set; } = "Europe/Berlin";
        public string Currency { get; set; } = "EUR";
        public string? PhoneE164 { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Request model for soft‑deleting a user. Includes a concurrency token.
    /// </summary>
    public sealed class UserDeleteDto
    {
        /// <summary>The identifier of the user to delete.</summary>
        public Guid Id { get; set; }

        /// <summary>Concurrency token to detect conflicting deletions.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Request model for changing a user's email address (Admin only).
    /// </summary>
    public sealed class UserChangeEmailDto
    {
        /// <summary>The identifier of the user whose email will be changed.</summary>
        public Guid Id { get; set; }

        /// <summary>The new email address. Must be unique and valid.</summary>
        public string NewEmail { get; set; } = string.Empty;
    }

    /// <summary>
    /// Admin-only request to set a user's password without requiring the current password.
    /// This is intended for helpdesk/admin flows in the Admin area.
    /// </summary>
    public sealed class UserAdminSetPasswordDto
    {
        /// <summary>Target user's identifier.</summary>
        public Guid Id { get; set; }

        /// <summary>
        /// New plain-text password to be hashed and stored.
        /// Must comply with password policy (validated in the validator).
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;
    }
}
