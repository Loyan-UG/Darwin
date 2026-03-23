using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services.Legal;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Shows a warning-first account deletion handoff flow for the Business app.
/// </summary>
public sealed class AccountDeletionViewModel : BaseViewModel
{
    private readonly ILegalLinkService _legalLinkService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountDeletionViewModel"/> class.
    /// </summary>
    /// <param name="legalLinkService">Service used to open the externally hosted deletion page.</param>
    public AccountDeletionViewModel(ILegalLinkService legalLinkService)
    {
        _legalLinkService = legalLinkService ?? throw new ArgumentNullException(nameof(legalLinkService));
        ContinueCommand = new AsyncCommand(OpenDeletionPageAsync, () => !IsBusy);
    }

    public AsyncCommand ContinueCommand { get; }

    private async Task OpenDeletionPageAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _legalLinkService.OpenAsync(LegalLinkKind.AccountDeletion, CancellationToken.None).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                RunOnMain(() => ErrorMessage = AppResources.AccountDeletionOpenFailed);
            }
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                ContinueCommand.RaiseCanExecuteChanged();
            });
        }
    }
}
