namespace Darwin.Contracts.Profile;

/// <summary>
/// Minimal editable customer profile.
/// </summary>
public sealed class CustomerProfile
{
    /// <summary>Server authoritative id of the current user.</summary>
    public Guid Id { get; init; }
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string? Locale { get; init; }
    public string? Timezone { get; init; }
    public string? PhoneE164 { get; init; }
}

