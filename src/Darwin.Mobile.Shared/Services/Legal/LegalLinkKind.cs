namespace Darwin.Mobile.Shared.Services.Legal;

/// <summary>
/// Enumerates the supported legal/compliance destinations that can be opened from the mobile apps.
/// </summary>
public enum LegalLinkKind
{
    /// <summary>
    /// The legally required impressum page.
    /// </summary>
    Impressum = 0,

    /// <summary>
    /// The privacy policy page.
    /// </summary>
    PrivacyPolicy = 1,

    /// <summary>
    /// The consumer terms page.
    /// </summary>
    ConsumerTerms = 2,

    /// <summary>
    /// The business terms page.
    /// </summary>
    BusinessTerms = 3,

    /// <summary>
    /// The account deletion information page.
    /// </summary>
    AccountDeletion = 4,

    /// <summary>
    /// The optional privacy choices page.
    /// </summary>
    PrivacyChoices = 5,

    /// <summary>
    /// The optional consumer pre-contract information page.
    /// </summary>
    ConsumerPreContractInfo = 6,

    /// <summary>
    /// The optional business legal information page.
    /// </summary>
    BusinessLegalInfo = 7
}
