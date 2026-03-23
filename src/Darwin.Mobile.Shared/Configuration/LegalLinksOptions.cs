using System;
using System.Collections.Generic;

namespace Darwin.Mobile.Shared.Configuration;

/// <summary>
/// Strongly typed configuration model that stores the canonical legal/compliance URLs used by the mobile apps.
/// </summary>
/// <remarks>
/// These URLs always point to the externally maintained legal pages on the Loyan website.
/// The mobile apps must never hardcode legal URLs inside pages or view models because the hosting paths
/// may vary across environments or legal revisions.
/// </remarks>
public sealed class LegalLinksOptions
{
    /// <summary>
    /// Gets or sets the absolute HTTPS URL of the impressum page.
    /// </summary>
    public string? ImpressumUrl { get; set; }

    /// <summary>
    /// Gets or sets the absolute HTTPS URL of the privacy policy page.
    /// </summary>
    public string? PrivacyPolicyUrl { get; set; }

    /// <summary>
    /// Gets or sets the absolute HTTPS URL of the consumer terms page.
    /// </summary>
    public string? ConsumerTermsUrl { get; set; }

    /// <summary>
    /// Gets or sets the absolute HTTPS URL of the business terms page.
    /// </summary>
    public string? BusinessTermsUrl { get; set; }

    /// <summary>
    /// Gets or sets the absolute HTTPS URL of the account deletion page.
    /// </summary>
    public string? AccountDeletionUrl { get; set; }

    /// <summary>
    /// Gets or sets the optional absolute HTTPS URL of a privacy choices/preferences page.
    /// </summary>
    public string? PrivacyChoicesUrl { get; set; }

    /// <summary>
    /// Gets or sets the optional absolute HTTPS URL of consumer pre-contract information.
    /// </summary>
    public string? ConsumerPreContractInfoUrl { get; set; }

    /// <summary>
    /// Gets or sets the optional absolute HTTPS URL of business-facing legal information.
    /// </summary>
    public string? BusinessLegalInfoUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether startup should fail immediately when required legal links are missing.
    /// </summary>
    public bool FailFastOnMissingRequiredLinks { get; set; }

    /// <summary>
    /// Returns the configured required link values keyed by their logical name.
    /// </summary>
    public IReadOnlyDictionary<string, string?> GetRequiredLinks()
    {
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [nameof(ImpressumUrl)] = ImpressumUrl,
            [nameof(PrivacyPolicyUrl)] = PrivacyPolicyUrl,
            [nameof(ConsumerTermsUrl)] = ConsumerTermsUrl,
            [nameof(BusinessTermsUrl)] = BusinessTermsUrl,
            [nameof(AccountDeletionUrl)] = AccountDeletionUrl
        };
    }
}
