namespace Darwin.Contracts.Identity
{
    /// <summary>
    /// Represents a request to change the current password of an authenticated user.
    /// </summary>
    public sealed class ChangePasswordRequest
    {
        /// <summary>
        /// Gets or sets the user's current password.
        /// </summary>
        public string CurrentPassword { get; init; } = default!;

        /// <summary>
        /// Gets or sets the new password desired by the user.
        /// </summary>
        public string NewPassword { get; init; } = default!;
    }
}
