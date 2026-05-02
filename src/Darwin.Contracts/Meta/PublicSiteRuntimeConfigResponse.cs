namespace Darwin.Contracts.Meta;

/// <summary>
/// Non-sensitive public site runtime configuration used by storefront clients.
/// </summary>
public sealed class PublicSiteRuntimeConfigResponse
{
    /// <summary>Default culture selected for public content when no request culture is supplied.</summary>
    public string DefaultCulture { get; init; } = "de-DE";

    /// <summary>Culture list currently enabled for public content entry and delivery.</summary>
    public IReadOnlyList<string> SupportedCultures { get; init; } = Array.Empty<string>();

    /// <summary>True when more than one supported culture is active.</summary>
    public bool MultilingualEnabled { get; init; }
}
