using System;

namespace Darwin.Shared.Constants
{
    /// <summary>
    ///     Centralized constants for well-known identifiers used across the application,
    ///     such as the Admin user id for auditing and reserved entity ids used by seed data or platform features.
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
        /// Represents the Admin user (used when no authenticated user is available).
        /// </summary>
        public static readonly Guid AdministratorUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        /// <summary>
        /// Well-known identifiers used by seeding and cross-layer references.
        /// Keep these values immutable once published.
        /// </summary>
        public static readonly Guid AdministratorsRoleId = Guid.Parse("00000000-0000-0000-0000-000000000000");

        /// <summary>
        /// Represents an anonymous user (not used for auditing writes).
        /// </summary>
        public static readonly Guid AnonymousUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        /// <summary>
        /// Default site members role (regular users).
        /// </summary>
        public static readonly Guid MembersRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        
    }
}
