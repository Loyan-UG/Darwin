namespace Darwin.Contracts.Profile;

/// <summary>
/// Represents the current customer's profile data returned by the public API.
/// This contract is used by mobile apps for profile screens and must remain stable.
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

    /// <summary>
    /// Concurrency token used for optimistic concurrency control.
    /// Serialized as Base64 in JSON by System.Text.Json.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

