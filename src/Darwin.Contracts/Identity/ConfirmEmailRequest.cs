namespace Darwin.Contracts.Identity
{
    /// <summary>
    /// Represents a request to confirm an account email using a one-time token.
    /// </summary>
    public sealed class ConfirmEmailRequest
    {
        /// <summary>
        /// Gets or sets the email address of the account being confirmed.
        /// </summary>
        public string Email { get; init; } = default!;

        /// <summary>
        /// Gets or sets the one-time confirmation token delivered to the user.
        /// </summary>
        public string Token { get; init; } = default!;
    }
}
