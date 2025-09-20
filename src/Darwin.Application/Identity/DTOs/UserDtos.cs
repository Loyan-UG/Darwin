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
    }

    /// <summary>Edit user input model.</summary>
    public sealed class UserEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>(); // For optimistic concurrency
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Locale { get; set; } = "de-DE";
        public string Timezone { get; set; } = "Europe/Berlin";
        public string Currency { get; set; } = "EUR";
        public string? PhoneE164 { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>Change password input.</summary>
    public sealed class UserChangePasswordDto
    {
        public Guid Id { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
