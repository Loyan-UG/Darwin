using System;

namespace Darwin.Shared.Constants
{
    /// <summary>
    ///     Centralized constants for well-known identifiers used across the application,
    ///     such as the system user id for auditing and reserved entity ids used by seed data or platform features.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Rationale:
    ///         <list type="bullet">
    ///             <item>Prevents magic GUIDs from being scattered in code.</item>
    ///             <item>Improves readability and maintains a single source of truth for reserved IDs.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Keep this file small and well-documented; avoid adding environment-specific values.
    ///     </para>
    /// </remarks>
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
