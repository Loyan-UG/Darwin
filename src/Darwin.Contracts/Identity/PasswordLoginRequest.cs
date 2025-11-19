namespace Darwin.Contracts.Identity
{

    /// <summary>
    /// Request payload for password-based authentication in the public Web API.
    /// This maps closely to PasswordLoginRequestDto in the Application layer.
    /// </summary>
    public sealed class PasswordLoginRequest
    {
        /// <summary>
        /// Gets or sets the email address used for login.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the plain-text password provided by the user.
        /// Never log this field.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an optional device identifier.
        /// When provided and JwtRequireDeviceBinding is enabled, the refresh token
        /// will be bound to this device.
        /// </summary>
        public string? DeviceId { get; set; }
    }
}
