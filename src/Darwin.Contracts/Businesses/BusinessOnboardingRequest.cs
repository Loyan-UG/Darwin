namespace Darwin.Contracts.Businesses;

/// <summary>
/// Request payload for self-service business onboarding.
/// This creates a new business tenant and links the current authenticated user as owner.
/// </summary>
public sealed class BusinessOnboardingRequest
{
    /// <summary>
    /// Public business display name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Optional legal name used for invoicing/compliance.
    /// </summary>
    public string? LegalName { get; init; }

    /// <summary>
    /// Optional tax identifier.
    /// </summary>
    public string? TaxId { get; init; }

    /// <summary>
    /// Optional short discovery description.
    /// </summary>
    public string? ShortDescription { get; init; }

    /// <summary>
    /// Optional business website URL.
    /// </summary>
    public string? WebsiteUrl { get; init; }

    /// <summary>
    /// Optional public contact email.
    /// </summary>
    public string? ContactEmail { get; init; }

    /// <summary>
    /// Optional public contact phone (E.164 preferred).
    /// </summary>
    public string? ContactPhoneE164 { get; init; }

    /// <summary>
    /// Optional business category key/name mapped by server.
    /// </summary>
    public string? CategoryKindKey { get; init; }

    /// <summary>
    /// Default business currency (ISO 4217), e.g. EUR.
    /// </summary>
    public string? DefaultCurrency { get; init; }

    /// <summary>
    /// Default business culture, e.g. de-DE.
    /// </summary>
    public string? DefaultCulture { get; init; }
}
