using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services.Legal;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Presents the centralized legal hub for the Business app.
/// </summary>
public sealed class LegalHubViewModel : BaseViewModel
{
    private readonly ILegalLinkService _legalLinkService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegalHubViewModel"/> class.
    /// </summary>
    /// <param name="legalLinkService">Service used to resolve and open configured legal links.</param>
    public LegalHubViewModel(ILegalLinkService legalLinkService)
    {
        _legalLinkService = legalLinkService ?? throw new ArgumentNullException(nameof(legalLinkService));

        OpenImpressumCommand = new AsyncCommand(() => OpenAsync(LegalLinkKind.Impressum), () => !IsBusy);
        OpenPrivacyPolicyCommand = new AsyncCommand(() => OpenAsync(LegalLinkKind.PrivacyPolicy), () => !IsBusy);
        OpenTermsCommand = new AsyncCommand(() => OpenAsync(LegalLinkKind.BusinessTerms), () => !IsBusy);
    }

    public AsyncCommand OpenImpressumCommand { get; }

    public AsyncCommand OpenPrivacyPolicyCommand { get; }

    public AsyncCommand OpenTermsCommand { get; }

    private async Task OpenAsync(LegalLinkKind linkKind)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _legalLinkService.OpenAsync(linkKind, CancellationToken.None).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                RunOnMain(() => ErrorMessage = AppResources.LegalOpenFailed);
            }
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                OpenImpressumCommand.RaiseCanExecuteChanged();
                OpenPrivacyPolicyCommand.RaiseCanExecuteChanged();
                OpenTermsCommand.RaiseCanExecuteChanged();
            });
        }
    }
}
