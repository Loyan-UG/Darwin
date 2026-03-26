namespace Darwin.Contracts.Identity
{
    /// <summary>
    /// Represents a request to issue or resend an email confirmation token for an account.
    /// </summary>
    public sealed class RequestEmailConfirmationRequest
    {
        /// <summary>
        /// Gets or sets the email address associated with the account that should receive the activation email.
        /// </summary>
        public string Email { get; init; } = default!;
    }
}
