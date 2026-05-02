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
    private CancellationTokenSource? _openCancellation;

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

    /// <summary>
    /// Cancels any in-flight legal link open operation when the page is no longer visible.
    /// </summary>
    /// <returns>A completed task because cancellation is signaled synchronously.</returns>
    public override Task OnDisappearingAsync()
    {
        CancelCurrentOpen();
        return Task.CompletedTask;
    }

    private async Task OpenAsync(LegalLinkKind linkKind)
    {
        if (IsBusy)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            RaiseCommandStates();
        });

        var openCancellation = BeginCurrentOpen();
        try
        {
            var result = await _legalLinkService.OpenAsync(linkKind, openCancellation.Token).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                RunOnMain(() => ErrorMessage = AppResources.LegalOpenFailed);
            }
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the legal hub intentionally cancels stale browser handoffs.
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCommandStates();
            });

            EndCurrentOpen(openCancellation);
        }
    }

    /// <summary>
    /// Starts a cancellable legal-link open operation and cancels any stale handoff still in-flight.
    /// </summary>
    private CancellationTokenSource BeginCurrentOpen()
    {
        var current = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _openCancellation, current);
        previous?.Cancel();
        return current;
    }

    /// <summary>
    /// Cancels the active legal-link open operation without disposing a token source still observed by service code.
    /// </summary>
    private void CancelCurrentOpen()
    {
        var current = Interlocked.Exchange(ref _openCancellation, null);
        current?.Cancel();
    }

    /// <summary>
    /// Releases a completed legal-link open operation when it still owns the active operation slot.
    /// </summary>
    /// <param name="openCancellation">Completed open token source.</param>
    private void EndCurrentOpen(CancellationTokenSource openCancellation)
    {
        if (ReferenceEquals(_openCancellation, openCancellation))
        {
            _openCancellation = null;
        }

        openCancellation.Dispose();
    }

    /// <summary>
    /// Refreshes all legal command states together to prevent opening multiple browser tabs at once.
    /// </summary>
    private void RaiseCommandStates()
    {
        OpenImpressumCommand.RaiseCanExecuteChanged();
        OpenPrivacyPolicyCommand.RaiseCanExecuteChanged();
        OpenTermsCommand.RaiseCanExecuteChanged();
    }
}
