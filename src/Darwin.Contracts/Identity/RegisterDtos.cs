namespace Darwin.Contracts.Identity;

/// <summary>Customer self-service registration request.</summary>
public sealed class RegisterRequest
{
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
}

/// <summary>Registration response minimal payload.</summary>
public sealed class RegisterResponse
{
    /// <summary>Display name for immediate UI use.</summary>
    public string DisplayName { get; init; } = default!;

    /// <summary>Indicates whether email confirmation was sent.</summary>
    public bool ConfirmationEmailSent { get; init; }
}
