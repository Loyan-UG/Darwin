using System;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Identity
{
    /// <summary>
    /// Represents a physical mobile device installation registered for a user.
    /// Used for push notifications, device binding policies, and security telemetry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Security: push tokens are secrets. Store them protected at rest in Infrastructure.
    /// </para>
    /// <para>
    /// Uniqueness: typically unique per (UserId, DeviceId). Enforce via unique index in Infrastructure.
    /// </para>
    /// </remarks>
    public sealed class UserDevice : BaseEntity
    {
        /// <summary>
        /// Owning user.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Stable client-generated installation id (not the push token).
        /// This value is also useful for JWT device binding scenarios.
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// Platform of the device (Android/iOS).
        /// </summary>
        public MobilePlatform Platform { get; set; } = MobilePlatform.Unknown;

        /// <summary>
        /// Push token (provider-specific). May be null if user disabled notifications.
        /// </summary>
        public string? PushToken { get; set; }

        /// <summary>
        /// UTC timestamp when the push token was last updated.
        /// </summary>
        public DateTime? PushTokenUpdatedAtUtc { get; set; }

        /// <summary>
        /// Whether notifications are enabled from the user's perspective.
        /// </summary>
        public bool NotificationsEnabled { get; set; } = true;

        /// <summary>
        /// Last time the device was seen/used (UTC). Useful for security telemetry and cleanup.
        /// </summary>
        public DateTime? LastSeenAtUtc { get; set; }

        /// <summary>
        /// App version reported by the client (semantic version string).
        /// </summary>
        public string? AppVersion { get; set; }

        /// <summary>
        /// Optional device model/manufacturer string (client-reported).
        /// </summary>
        public string? DeviceModel { get; set; }

        /// <summary>
        /// Whether this record is active. Deactivate instead of hard delete to preserve audit trails.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Optional navigation to the user.
        /// </summary>
        public User? User { get; private set; }
    }
}
