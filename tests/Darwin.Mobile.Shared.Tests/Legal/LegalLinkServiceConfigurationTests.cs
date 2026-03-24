using Darwin.Mobile.Shared.Configuration;
using Darwin.Mobile.Shared.Services.Legal;
using FluentAssertions;

namespace Darwin.Mobile.Shared.Tests.Legal;

/// <summary>
/// Verifies configuration-driven legal-link resolution used by the mobile apps.
/// </summary>
public sealed class LegalLinkServiceConfigurationTests
{
    /// <summary>
    /// Ensures the account-deletion destination is resolved from centralized configuration rather than hardcoded UI strings.
    /// </summary>
    [Fact]
    public void ResolveUri_Should_ReturnConfiguredAccountDeletionUrl_WhenAccountDeletionLinkIsRequested()
    {
        var options = new LegalLinksOptions
        {
            ImpressumUrl = "https://loyan.de/impressum",
            PrivacyPolicyUrl = "https://loyan.de/datenschutz",
            ConsumerTermsUrl = "https://loyan.de/nutzungsbedingungen-consumer",
            BusinessTermsUrl = "https://loyan.de/nutzungsbedingungen-business",
            AccountDeletionUrl = "https://loyan.de/konto-loeschen"
        };

        var service = new LegalLinkService(options);

        var result = service.ResolveUri(LegalLinkKind.AccountDeletion);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AbsoluteUri.Should().Be("https://loyan.de/konto-loeschen");
    }
}
