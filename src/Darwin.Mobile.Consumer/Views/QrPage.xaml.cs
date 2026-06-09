using Darwin.Mobile.Consumer.ViewModels;
using Darwin.Mobile.Consumer.Services.Navigation;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Displays the rotating QR code for the consumer.
///
/// Navigation contract:
/// - Accepts a `businessId` query parameter (GUID).
/// - Accepts optional `businessName` and `justJoined` parameters for user context messaging.
/// - Sets the business context on the view model before OnAppearing refresh.
/// </summary>
public partial class QrPage : IQueryAttributable
{
    private readonly QrViewModel _viewModel;
    private readonly IQrNavigationContext _navigationContext;
    private int _appearanceRefreshInProgress;
    private int _pendingAppearanceRefresh;

    public QrPage(QrViewModel viewModel, IQrNavigationContext navigationContext)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _navigationContext = navigationContext ?? throw new ArgumentNullException(nameof(navigationContext));
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var appliedBusinessContext = false;

        if (query.TryGetValue("businessId", out var rawBusinessId))
        {
            // Shell may pass query values either as typed objects (Guid) or as strings,
            // depending on how navigation parameters were supplied.
            if (rawBusinessId is Guid businessId)
            {
                _viewModel.SetBusiness(businessId);
                appliedBusinessContext = true;
            }
            else if (rawBusinessId is string businessIdText && Guid.TryParse(SafeUnescape(businessIdText), out var parsedBusinessId))
            {
                _viewModel.SetBusiness(parsedBusinessId);
                appliedBusinessContext = true;
            }
        }

        if (appliedBusinessContext)
        {
            _navigationContext.Clear();
        }

        if (query.TryGetValue("businessName", out var rawBusinessName))
        {
            // Keep business name binding resilient for both plain values and encoded string values.
            var businessName = rawBusinessName as string;
            if (!string.IsNullOrWhiteSpace(businessName))
            {
                _viewModel.SetBusinessDisplayName(SafeUnescape(businessName));
            }
        }

        var joined = false;
        if (query.TryGetValue("joined", out var rawJoined) ||
            query.TryGetValue("justJoined", out rawJoined))
        {
            // Accept both the canonical joined query key and the older justJoined key
            // so QR navigation remains compatible across Discover, Rewards, and post-join flows.
            joined = rawJoined switch
            {
                bool b => b,
                string joinedText => string.Equals(joinedText, "true", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(joinedText, "1", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        if (!appliedBusinessContext && TryApplyNavigationContextFallback(out var fallbackJoined))
        {
            joined = fallbackJoined;
        }

        _viewModel.SetJoinedStatus(joined);

        // Trigger immediate first-load session creation after navigation parameters are applied.
        // This prevents a blank QR state when navigation timing causes OnAppearing to run earlier.
        _ = RunAppearingSafelyAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = TryApplyNavigationContextFallback(out _);
        await RunAppearingSafelyAsync();
    }

    protected override async void OnDisappearing()
    {
        try
        {
            await _viewModel.OnDisappearingAsync();
        }
        catch
        {
            // Disappearing cleanup should never crash navigation away from the QR page.
        }
        finally
        {
            base.OnDisappearing();
        }
    }

    /// <summary>
    /// Runs QR session refresh without allowing async-void lifecycle dispatch to surface unexpected exceptions.
    /// </summary>
    private async Task RunAppearingSafelyAsync()
    {
        if (Interlocked.Exchange(ref _appearanceRefreshInProgress, 1) == 1)
        {
            Interlocked.Exchange(ref _pendingAppearanceRefresh, 1);
            return;
        }

        try
        {
            do
            {
                Interlocked.Exchange(ref _pendingAppearanceRefresh, 0);
                await _viewModel.OnAppearingAsync();
            }
            while (Interlocked.Exchange(ref _pendingAppearanceRefresh, 0) == 1);
        }
        catch
        {
            // QR refresh failures are surfaced by the ViewModel state and should not crash navigation.
        }
        finally
        {
            Interlocked.Exchange(ref _appearanceRefreshInProgress, 0);

            if (Interlocked.Exchange(ref _pendingAppearanceRefresh, 0) == 1)
            {
                _ = RunAppearingSafelyAsync();
            }
        }
    }

    /// <summary>
    /// Applies the last explicit QR navigation context when Shell query parameters were not delivered.
    /// </summary>
    private bool TryApplyNavigationContextFallback(out bool joined)
    {
        joined = false;

        if (!_navigationContext.TryConsume(out var context))
        {
            return false;
        }

        _viewModel.SetBusiness(context.BusinessId);
        _viewModel.SetBusinessDisplayName(context.BusinessName);
        joined = context.Joined;
        _viewModel.SetJoinedStatus(joined);
        return true;
    }

    /// <summary>
    /// Decodes route values defensively so malformed navigation input cannot crash QR context setup.
    /// </summary>
    /// <param name="raw">Raw route value supplied by Shell.</param>
    /// <returns>Decoded and trimmed value, or the trimmed raw value when decoding is not possible.</returns>
    private static string SafeUnescape(string raw)
    {
        try
        {
            return Uri.UnescapeDataString(raw).Trim();
        }
        catch (UriFormatException)
        {
            return raw.Trim();
        }
    }
}
