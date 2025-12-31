namespace Darwin.Contracts.Identity
{
    /// <summary>
    /// Represents a request to initiate a password reset for a user account.
    /// </summary>
    public sealed class RequestPasswordResetRequest
    {
        /// <summary>
        /// Gets or sets the email address associated with the account to reset.
        /// </summary>
        public string Email { get; init; } = default!;
    }
}
