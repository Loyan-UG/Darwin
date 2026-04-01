using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Darwin.Application.Identity.DTOs;
using Darwin.WebAdmin.Localization;

namespace Darwin.WebAdmin.ViewModels.Identity
{
    /// <summary>
    /// View model for the users listing page with paging and search.
    /// </summary>
    public sealed class UsersListVm
    {
        public List<UserListItemVm> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public UserQueueFilter Filter { get; set; } = UserQueueFilter.All;
        public UserOpsSummaryVm Summary { get; set; } = new();
        public List<UserSupportPlaybookVm> Playbooks { get; set; } = new();
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
    }


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
        public string? PhoneE164 { get; set; }

        /// <summary>Whether the account is active (enabled).</summary>
        public bool IsActive { get; set; }

        /// <summary>Whether this user is system-protected and cannot be deleted.</summary>
        public bool IsSystem { get; set; }

        public bool EmailConfirmed { get; set; }
        public DateTime? LockoutEndUtc { get; set; }
        public int MobileDeviceCount { get; set; }
        public bool IsLockedOut => LockoutEndUtc.HasValue && LockoutEndUtc.Value > DateTime.UtcNow;

        /// <summary>Concurrency token to detect conflicting updates.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class UserOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int UnconfirmedCount { get; set; }
        public int LockedCount { get; set; }
        public int InactiveCount { get; set; }
        public int MobileLinkedCount { get; set; }
    }

    public sealed class UserSupportPlaybookVm
    {
        public string Title { get; set; } = string.Empty;
        public string ScopeNote { get; set; } = string.Empty;
        public string OperatorAction { get; set; } = string.Empty;
        public string FollowUp { get; set; } = string.Empty;
    }

    /// <summary>
    /// Shared admin editor model for user create and edit screens.
    /// </summary>
    public abstract class UserEditorVm
    {
        /// <summary>Given name; optional.</summary>
        public string? FirstName { get; set; }

        /// <summary>Family name; optional.</summary>
        public string? LastName { get; set; }

        /// <summary>Preferred culture code.</summary>
        [Required(ErrorMessage = "The {0} field is required.")]
        [Display(Name = "Locale")]
        public string Locale { get; set; } = AdminCultureCatalog.DefaultCulture;

        /// <summary>Preferred time zone.</summary>
        [Required(ErrorMessage = "The {0} field is required.")]
        [Display(Name = "TimeZone")]
        public string Timezone { get; set; } = "Europe/Berlin";

        /// <summary>Preferred currency code (ISO 4217).</summary>
        [Required(ErrorMessage = "The {0} field is required.")]
        [Display(Name = "Currency")]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = "EUR";

        /// <summary>Phone number in E.164 format; optional.</summary>
        [Display(Name = "Phone")]
        [Phone]
        public string? PhoneE164 { get; set; }
    }

    /// <summary>
    /// View model used to create a new user in the admin area.
    /// </summary>
    public sealed class UserCreateVm : UserEditorVm
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>Plain text password. Must be at least 8 characters.</summary>
        [Required(ErrorMessage = "The {0} field is required.")]
        [Display(Name = "Password")]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        /// <summary>Initial active flag; default true.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Marks the user as system-protected; use sparingly.</summary>
        public bool IsSystem { get; set; } = false;

        /// <summary>Concurrency token to detect conflicting updates.</summary>
    }

    /// <summary>
    /// View model used to edit an existing user (admin area).
    /// Note that Email and Username are not editable here.
    /// </summary>
    public sealed class UserEditVm : UserEditorVm
    {
        public Guid Id { get; set; }

        /// <summary>Concurrency token to detect conflicting updates.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        /// <summary>Current login email shown in the admin UI for context-sensitive actions.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Indicates whether the user's primary email address is confirmed.</summary>
        public bool EmailConfirmed { get; set; }

        /// <summary>
        /// When set to a future UTC timestamp, the account is currently locked for sign-in.
        /// </summary>
        public DateTime? LockoutEndUtc { get; set; }

        /// <summary>Active flag; deactivating disables login.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Returns whether the user is currently locked out.</summary>
        public bool IsLockedOut => LockoutEndUtc.HasValue && LockoutEndUtc.Value > DateTime.UtcNow;
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

        [Required(ErrorMessage = "The {0} field is required.")]
        [Display(Name = "Locale")]
        public string Locale { get; set; } = AdminCultureCatalog.DefaultCulture;

        [Required(ErrorMessage = "The {0} field is required.")]
        [Display(Name = "TimeZone")]
        public string Timezone { get; set; } = "Europe/Berlin";

        [Required(ErrorMessage = "The {0} field is required.")]
        [Display(Name = "Currency")]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = "EUR";

        [Display(Name = "Phone")]
        [Phone]
        public string? PhoneE164 { get; set; }
    }

    /// <summary>
    /// View model for changing a user's email (admin).
    /// </summary>
    public sealed class UserChangeEmailVm
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Current email displayed back to the operator so invalid submissions can re-render without losing context.
        /// </summary>
        public string CurrentEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "The {0} field is required.")]
        [Display(Name = "NewEmail")]
        [EmailAddress]
        public string NewEmail { get; set; } = string.Empty;
    }

    /// <summary>
    /// View model for soft-deleting a user. Contains only an identifier and concurrency token.
    /// </summary>
    public sealed class UserDeleteVm
    {
        public Guid Id { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// View model used by the Admin "Change Password" flow.
    /// Admins can set a new password for a user without knowing the current password.
    /// </summary>
    public sealed class UserChangePasswordVm
    {
        /// <summary>
        /// Target user id whose password is being set by an administrator.
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// Email is shown on the page for visual verification (to avoid changing the wrong user's password).
        /// Not posted back as an authoritative value; it's display-only.
        /// </summary>
        [Display(Name = "Email")]
        public string? Email { get; set; }

        /// <summary>
        /// New password to be set for the user.
        /// UI should enforce minimum length of 8 characters; server-side validation is performed in Application layer.
        /// </summary>
        [Required(ErrorMessage = "The {0} field is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [Display(Name = "NewPassword")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Confirmation of the new password. Must match <see cref="NewPassword"/>.
        /// </summary>
        [Required(ErrorMessage = "The {0} field is required.")]
        [MinLength(8)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "ConfirmNewPassword")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }


}
