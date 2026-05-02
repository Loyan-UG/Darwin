using System.Threading.Tasks;
using Darwin.Mobile.Business.ViewModels;
using Microsoft.Maui.Controls;
using System.Threading;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Code-behind for <see cref="LoginPage"/>.
/// This page disables Shell navigation and flyout so that the login screen is shown
/// as a focused, standalone entry point for authentication.
/// </summary>
public partial class LoginPage
{
    private readonly LoginViewModel _viewModel;
    private CancellationTokenSource? _errorScrollCancellation;
    private bool _isErrorScrollSubscribed;

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));

        // Set the binding context to the injected view model
        BindingContext = _viewModel;

        // Make this login page standalone:
        // - Hide the top navigation bar
        // - Disable the flyout menu for this page
        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);
        Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);

        SubscribeErrorScroll();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SubscribeErrorScroll();
        ResetErrorScrollCancellation();
        _ = RunAppearingSafelyAsync();
    }

    protected override async void OnDisappearing()
    {
        try
        {
            CancelErrorScroll();
            UnsubscribeErrorScroll();
            await _viewModel.OnDisappearingAsync();
        }
        catch
        {
            // Disappearing cleanup should never crash navigation away from login.
        }
        finally
        {
            base.OnDisappearing();
        }
    }

    /// <summary>
    /// Runs login appearance work without letting async-void lifecycle dispatch surface unexpected exceptions.
    /// </summary>
    private async Task RunAppearingSafelyAsync()
    {
        try
        {
            await _viewModel.OnAppearingAsync();
        }
        catch
        {
            // Login startup refresh failures are handled by the next explicit user action.
        }
    }

    /// <summary>
    /// Scrolls to top so validation/network errors remain visible even if keyboard is open.
    /// </summary>
    private async void OnErrorBecameVisibleRequested()
    {
        try
        {
            if (RootScrollView is null)
            {
                return;
            }

            var token = _errorScrollCancellation?.Token ?? CancellationToken.None;
            await Task.Delay(40, token);
            token.ThrowIfCancellationRequested();
            await RootScrollView.ScrollToAsync(0, 0, true);
        }
        catch (OperationCanceledException)
        {
            // The page is no longer visible, so the delayed feedback scroll is no longer relevant.
        }
        catch
        {
            // Non-critical UI behavior; never crash login flow for this.
        }
    }

    /// <summary>
    /// Subscribes the feedback-scroll request exactly once for the visible login page.
    /// </summary>
    private void SubscribeErrorScroll()
    {
        if (_isErrorScrollSubscribed)
        {
            return;
        }

        _viewModel.ErrorBecameVisibleRequested += OnErrorBecameVisibleRequested;
        _isErrorScrollSubscribed = true;
    }

    /// <summary>
    /// Detaches the feedback-scroll request so returning from child pages cannot create duplicate handlers.
    /// </summary>
    private void UnsubscribeErrorScroll()
    {
        if (!_isErrorScrollSubscribed)
        {
            return;
        }

        _viewModel.ErrorBecameVisibleRequested -= OnErrorBecameVisibleRequested;
        _isErrorScrollSubscribed = false;
    }

    /// <summary>
    /// Starts a fresh cancellation scope for delayed feedback scrolling while the login page is visible.
    /// </summary>
    private void ResetErrorScrollCancellation()
    {
        CancelErrorScroll();
        _errorScrollCancellation = new CancellationTokenSource();
    }

    /// <summary>
    /// Cancels delayed feedback scrolling when navigation leaves the login page.
    /// </summary>
    private void CancelErrorScroll()
    {
        var cancellation = Interlocked.Exchange(ref _errorScrollCancellation, null);
        if (cancellation is null)
        {
            return;
        }

        cancellation.Cancel();
        cancellation.Dispose();
    }
}
