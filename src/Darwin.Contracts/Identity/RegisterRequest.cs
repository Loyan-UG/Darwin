namespace Darwin.Contracts.Identity
{
    /// <summary>
    /// Customer self-service registration request.
    /// </summary>
    public sealed class RegisterRequest
    {
        public string FirstName { get; init; } = default!;
        public string LastName { get; init; } = default!;
        public string Email { get; init; } = default!;
        public string Password { get; init; } = default!;
    }

    
}