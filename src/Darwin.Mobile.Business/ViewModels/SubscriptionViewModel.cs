using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.ViewModels;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Threading.Tasks;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Coordinates business subscription self-service actions from mobile settings.
/// 
/// Current scope (Phase 1 of subscription UX):
/// - Display whether a billing portal URL is configured for this environment.
/// - Open the configured billing portal in system browser for plan/payment management.
/// 
/// Notes:
/// - This ViewModel does not persist or mutate subscription data directly.
/// - Billing authority remains server-side (Stripe + backend ownership).
/// - Mobile acts as an operator entry point that forwards users to managed billing UI.
/// </summary>
public sealed class SubscriptionViewModel : BaseViewModel
{
    private readonly ApiOptions _apiOptions;

    private bool _isPortalConfigured;
    private string _portalHint = string.Empty;

    public SubscriptionViewModel(ApiOptions apiOptions)
    {
        _apiOptions = apiOptions ?? throw new ArgumentNullException(nameof(apiOptions));
        OpenBillingPortalCommand = new AsyncCommand(OpenBillingPortalAsync, () => !IsBusy && IsPortalConfigured);
    }

    /// <summary>
    /// Gets a value indicating whether a billing portal URL is configured.
    /// </summary>
    public bool IsPortalConfigured
    {
        get => _isPortalConfigured;
        private set
        {
            if (SetProperty(ref _isPortalConfigured, value))
            {
                OpenBillingPortalCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets a user-facing hint that explains subscription action availability.
    /// </summary>
    public string PortalHint
    {
        get => _portalHint;
        private set => SetProperty(ref _portalHint, value);
    }

    /// <summary>
    /// Opens Stripe/customer billing portal URL in external browser.
    /// </summary>
    public AsyncCommand OpenBillingPortalCommand { get; }

    public override Task OnAppearingAsync()
    {
        RunOnMain(() =>
        {
            var url = _apiOptions.BusinessBillingPortalUrl?.Trim();
            IsPortalConfigured = Uri.TryCreate(url, UriKind.Absolute, out _);

            PortalHint = IsPortalConfigured
                ? AppResources.SubscriptionPortalReadyHint
                : AppResources.SubscriptionPortalMissingHint;

            ErrorMessage = null;
        });

        return Task.CompletedTask;
    }

    private async Task OpenBillingPortalAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var url = _apiOptions.BusinessBillingPortalUrl?.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var portalUri))
        {
            RunOnMain(() => ErrorMessage = AppResources.SubscriptionPortalMissingHint);
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
        });

        try
        {
            // Open system browser so payment screens remain in the provider-controlled secure surface.
            await Browser.OpenAsync(portalUri, BrowserLaunchMode.SystemPreferred);
        }
        catch
        {
            // Keep messaging generic and user-facing (no raw technical details).
            RunOnMain(() => ErrorMessage = AppResources.SubscriptionPortalOpenFailed);
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                OpenBillingPortalCommand.RaiseCanExecuteChanged();
            });
        }
    }
}
