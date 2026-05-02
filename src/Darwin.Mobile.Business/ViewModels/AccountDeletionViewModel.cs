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
    private CancellationTokenSource? _openCancellation;

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

    /// <summary>
    /// Cancels any in-flight deletion-page handoff when the page is no longer visible.
    /// </summary>
    /// <returns>A completed task because cancellation is signaled synchronously.</returns>
    public override Task OnDisappearingAsync()
    {
        CancelCurrentOpen();
        return Task.CompletedTask;
    }

    private async Task OpenDeletionPageAsync()
    {
        if (IsBusy)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            ContinueCommand.RaiseCanExecuteChanged();
        });

        var openCancellation = BeginCurrentOpen();
        try
        {
            var result = await _legalLinkService.OpenAsync(LegalLinkKind.AccountDeletion, openCancellation.Token).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                RunOnMain(() => ErrorMessage = AppResources.AccountDeletionOpenFailed);
            }
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the deletion handoff intentionally cancels stale browser handoffs.
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                ContinueCommand.RaiseCanExecuteChanged();
            });

            EndCurrentOpen(openCancellation);
        }
    }

    /// <summary>
    /// Starts a cancellable deletion-page handoff and cancels any stale handoff still in-flight.
    /// </summary>
    private CancellationTokenSource BeginCurrentOpen()
    {
        var current = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _openCancellation, current);
        previous?.Cancel();
        return current;
    }

    /// <summary>
    /// Cancels the active deletion-page handoff without disposing a token source still observed by service code.
    /// </summary>
    private void CancelCurrentOpen()
    {
        var current = Interlocked.Exchange(ref _openCancellation, null);
        current?.Cancel();
    }

    /// <summary>
    /// Releases a completed deletion-page handoff when it still owns the active operation slot.
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
}
