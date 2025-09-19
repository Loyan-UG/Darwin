using System;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Identity
{
    /// <summary>
    /// Application user entity (custom Identity). Contains authentication data,
    /// profile fields (merged from previous UserProfile), preferences, consents,
    /// and attribution metadata. Designed for B2C/B2B and future CRM needs.
    /// </summary>
    public sealed class User : BaseEntity
    {
        // System flags
        /// <summary>When true, record is system-protected (not deletable in Admin).</summary>
        public bool IsSystem { get; set; }

        /// <summary>Whether the account is active (enabled). Admin grids often filter on this.</summary>
        public bool IsActive { get; set; } = true;

        // Login identity
        /// <summary>Unique username (can be same as email for simple setups).</summary>
        public string UserName { get; set; } = string.Empty;
        /// <summary>Normalized username (usually upper-cased).</summary>
        public string NormalizedUserName { get; set; } = string.Empty;

        /// <summary>Primary email used for login/notifications.</summary>
        public string Email { get; set; } = string.Empty;
        /// <summary>Normalized email (usually upper-cased).</summary>
        public string NormalizedEmail { get; set; } = string.Empty;
        /// <summary>Whether email has been confirmed.</summary>
        public bool EmailConfirmed { get; set; }

        /// <summary>Password hash (include salt inside hash or algorithm marker as per policy).</summary>
        public string PasswordHash { get; set; } = string.Empty;
        /// <summary>Random token that changes whenever security-sensitive fields change (logout old sessions).</summary>
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>Phone number in E.164 format.</summary>
        public string? PhoneE164 { get; set; }
        /// <summary>Whether phone number has been confirmed.</summary>
        public bool PhoneNumberConfirmed { get; set; }

        /// <summary>Enables TOTP/2FA for this user.</summary>
        public bool TwoFactorEnabled { get; set; }
        /// <summary>When set, the user is locked out until this UTC time.</summary>
        public DateTime? LockoutEndUtc { get; set; }
        /// <summary>Count of recent failed access attempts.</summary>
        public int AccessFailedCount { get; set; }

        // Profile (merged from previous UserProfile)
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Company { get; set; }
        public string? VatId { get; set; }

        /// <summary>Default billing address id (if stored as a reusable address).</summary>
        public Guid? DefaultBillingAddressId { get; set; }
        /// <summary>Default shipping address id (if stored as a reusable address).</summary>
        public Guid? DefaultShippingAddressId { get; set; }

        // Consent & Preferences
        public bool MarketingConsent { get; set; }
        /// <summary>Per-channel opt-ins JSON, e.g., {"Email":true,"SMS":false,"WhatsApp":true}.</summary>
        public string ChannelsOptInJson { get; set; } = "{}";
        public DateTime? AcceptsTermsAtUtc { get; set; }

        /// <summary>Preferred culture (IETF tag), e.g., "de-DE".</summary>
        public string Locale { get; set; } = "de-DE";
        /// <summary>Preferred currency for price display.</summary>
        public string Currency { get; set; } = "EUR";
        /// <summary>Preferred time zone for display.</summary>
        public string Timezone { get; set; } = "Europe/Berlin";

        // Attribution / CRM
        public string? AnonymousId { get; set; }
        public string FirstTouchUtmJson { get; set; } = "{}";
        public string LastTouchUtmJson { get; set; } = "{}";
        public string? Tags { get; set; }
        public string ExternalIdsJson { get; set; } = "{}";


        // Navigations
        public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
        public ICollection<UserLogin> Logins { get; private set; } = new List<UserLogin>();
        public ICollection<UserToken> Tokens { get; private set; } = new List<UserToken>();
        public ICollection<UserTwoFactorSecret> TwoFactorSecrets { get; private set; } = new List<UserTwoFactorSecret>();

        // EF parameterless constructor (kept protected to avoid misuse)
        private User() { }

        // Domain helper (optional)
        public void AddRole(Guid roleId)
        {
            if (!HasRole(roleId))
                UserRoles.Add(new UserRole(Id, roleId));
        }
        public bool HasRole(Guid roleId) => ((List<UserRole>)UserRoles).Exists(ur => ur.RoleId == roleId);
        public void RemoveRole(Guid roleId)
        {
            var found = ((List<UserRole>)UserRoles).Find(ur => ur.RoleId == roleId);
            if (found != null) UserRoles.Remove(found);
        }
    }
}
