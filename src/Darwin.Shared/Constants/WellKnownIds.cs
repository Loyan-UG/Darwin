using System;

namespace Darwin.Shared.Constants
{
    /// <summary>
    /// Well-known identifiers used across the system.
    /// </summary>
    public static class WellKnownIds
    {
        /// <summary>
        /// Represents the system user (used when no authenticated user is available).
        /// </summary>
        public static readonly Guid SystemUserId = new Guid("00000000-0000-0000-0000-000000000001");

        /// <summary>
        /// Represents an anonymous user (not used for auditing writes).
        /// </summary>
        public static readonly Guid AnonymousUserId = new Guid("00000000-0000-0000-0000-000000000000");
    }
}
