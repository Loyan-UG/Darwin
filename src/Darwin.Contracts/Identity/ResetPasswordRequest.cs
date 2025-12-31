namespace Darwin.Contracts.Identity
{
    /// <summary>
    /// Represents a request to complete a password reset with a verification token.
    /// </summary>
    public sealed class ResetPasswordRequest
    {
        /// <summary>
        /// Gets or sets the email address associated with the account being reset.
        /// </summary>
        public string Email { get; init; } = default!;

        /// <summary>
        /// Gets or sets the verification token issued to the user for resetting the password.
        /// </summary>
        public string Token { get; init; } = default!;

        /// <summary>
        /// Gets or sets the new password that will replace the old password upon a successful reset.
        /// </summary>
        public string NewPassword { get; init; } = default!;
    }
}
