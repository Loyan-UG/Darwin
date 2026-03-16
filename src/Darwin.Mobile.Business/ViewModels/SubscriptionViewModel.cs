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
/// - Enforce HTTPS portal URL requirement before enabling actions.
/// - Open the configured billing portal in system browser for plan/payment management.
/// - Allow operators to copy portal URL for troubleshooting and secure sharing.
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
    private string _portalUrlText = string.Empty;

    public SubscriptionViewModel(ApiOptions apiOptions)
    {
        _apiOptions = apiOptions ?? throw new ArgumentNullException(nameof(apiOptions));

        OpenBillingPortalCommand = new AsyncCommand(OpenBillingPortalAsync, () => !IsBusy && IsPortalConfigured);
        CopyBillingPortalUrlCommand = new AsyncCommand(CopyBillingPortalUrlAsync, () => !IsBusy && IsPortalConfigured);
    }

    /// <summary>
    /// Gets a value indicating whether a billing portal URL is configured and valid.
    /// </summary>
    public bool IsPortalConfigured
    {
        get => _isPortalConfigured;
        private set
        {
            if (SetProperty(ref _isPortalConfigured, value))
            {
                OpenBillingPortalCommand.RaiseCanExecuteChanged();
                CopyBillingPortalUrlCommand.RaiseCanExecuteChanged();
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
    /// Gets a normalized billing portal URL text for display and copy actions.
    /// </summary>
    public string PortalUrlText
    {
        get => _portalUrlText;
        private set => SetProperty(ref _portalUrlText, value);
    }

    /// <summary>
    /// Opens Stripe/customer billing portal URL in external browser.
    /// </summary>
    public AsyncCommand OpenBillingPortalCommand { get; }

    /// <summary>
    /// Copies configured billing portal URL to clipboard for operator troubleshooting.
    /// </summary>
    public AsyncCommand CopyBillingPortalUrlCommand { get; }

    public override Task OnAppearingAsync()
    {
        RunOnMain(() =>
        {
            var portalUri = GetValidatedPortalUri();
            IsPortalConfigured = portalUri is not null;
            PortalUrlText = portalUri?.AbsoluteUri ?? string.Empty;

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

        var portalUri = GetValidatedPortalUri();
        if (portalUri is null)
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
                CopyBillingPortalUrlCommand.RaiseCanExecuteChanged();
            });
        }
    }

    private async Task CopyBillingPortalUrlAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var portalUri = GetValidatedPortalUri();
        if (portalUri is null)
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
            await Clipboard.SetTextAsync(portalUri.AbsoluteUri);
            RunOnMain(() => PortalHint = AppResources.SubscriptionPortalCopiedHint);
        }
        catch
        {
            RunOnMain(() => ErrorMessage = AppResources.SubscriptionPortalCopyFailed);
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                OpenBillingPortalCommand.RaiseCanExecuteChanged();
                CopyBillingPortalUrlCommand.RaiseCanExecuteChanged();
            });
        }
    }

    /// <summary>
    /// Resolves and validates configured billing portal URL.
    /// Only HTTPS absolute URIs are accepted to prevent insecure redirects.
    /// </summary>
    private Uri? GetValidatedPortalUri()
    {
        var rawUrl = _apiOptions.BusinessBillingPortalUrl?.Trim();
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var portalUri))
        {
            return null;
        }

        if (!string.Equals(portalUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return portalUri;
    }
}
