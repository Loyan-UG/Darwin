using System;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Identity
{
    /// <summary>
    /// External login link (Google, Microsoft, etc.). 
    /// One user can have multiple external provider bindings.
    /// </summary>
    public sealed class UserLogin : BaseEntity
    {
        public Guid UserId { get; set; }

        /// <summary>Provider name, e.g., "Google", "Microsoft".</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>Provider-specific unique key for this user.</summary>
        public string ProviderKey { get; set; } = string.Empty;

        /// <summary>Optional display name/data returned by provider.</summary>
        public string? DisplayName { get; set; }


        public User? User { get; private set; }

        private UserLogin() { }

        public UserLogin(Guid userId, string provider, string providerKey, string? displayName = null)
        {
            UserId = userId;
            Provider = provider;
            ProviderKey = providerKey;
            DisplayName = displayName;
        }
    }
}
