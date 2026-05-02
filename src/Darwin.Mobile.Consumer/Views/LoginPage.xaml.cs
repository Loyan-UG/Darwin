using Darwin.Mobile.Consumer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Code-behind for the login page.
/// </summary>
public partial class LoginPage
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LoginViewModel _viewModel;
    private CancellationTokenSource? _errorScrollCancellation;
    private bool _isErrorScrollSubscribed;
    private int _navigationInProgress;

    public LoginPage(LoginViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;

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
            // Disappearing is an async-void MAUI lifecycle hook; cancellation cleanup must not crash login.
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
            // Login prefill/startup refresh failures are handled by the next explicit user action.
        }
    }

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
        }
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        await PushPageSafelyAsync<RegisterPage>();
    }

    private async void OnRegisterTapped(object? sender, TappedEventArgs e)
    {
        await PushPageSafelyAsync<RegisterPage>();
    }

    private async void OnForgotPasswordClicked(object? sender, EventArgs e)
    {
        await PushPageSafelyAsync<ForgotPasswordPage>();
    }

    private async void OnForgotPasswordTapped(object? sender, TappedEventArgs e)
    {
        await PushPageSafelyAsync<ForgotPasswordPage>();
    }

    private async void OnOpenActivationClicked(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _navigationInProgress, 1) == 1)
        {
            return;
        }

        try
        {
            var page = _serviceProvider.GetService<ActivationPage>()
                ?? throw new InvalidOperationException("Activation page is not registered in DI.");

            if (BindingContext is LoginViewModel loginViewModel &&
                page.BindingContext is ActivationViewModel activationViewModel)
            {
                activationViewModel.ApplyPrefill(loginViewModel.Email);
            }

            await Navigation.PushAsync(page);
        }
        catch
        {
        }
        finally
        {
            Interlocked.Exchange(ref _navigationInProgress, 0);
        }
    }

    private async void OnLegalHubClicked(object? sender, EventArgs e)
    {
        await PushPageSafelyAsync<LegalHubPage>();
    }

    private async void OnLegalHubTapped(object? sender, TappedEventArgs e)
    {
        await PushPageSafelyAsync<LegalHubPage>();
    }

    private void OnEmailEntryFocused(object? sender, FocusEventArgs e)
    {
        EmailInputLayout.Stroke = (Color)Application.Current!.Resources["BrandGold500"];
    }

    private void OnEmailEntryUnfocused(object? sender, FocusEventArgs e)
    {
        EmailInputLayout.Stroke = (Color)Application.Current!.Resources["Neutral100"];
    }

    private void OnPasswordEntryFocused(object? sender, FocusEventArgs e)
    {
        PasswordInputLayout.Stroke = (Color)Application.Current!.Resources["BrandGold500"];
    }

    private void OnPasswordEntryUnfocused(object? sender, FocusEventArgs e)
    {
        PasswordInputLayout.Stroke = (Color)Application.Current!.Resources["Neutral100"];
    }

    private async Task PushPageSafelyAsync<TPage>() where TPage : Page
    {
        if (Interlocked.Exchange(ref _navigationInProgress, 1) == 1)
        {
            return;
        }

        try
        {
            var page = _serviceProvider.GetService<TPage>()
                ?? throw new InvalidOperationException($"{typeof(TPage).Name} is not registered in DI.");

            await Navigation.PushAsync(page);
        }
        catch
        {
        }
        finally
        {
            Interlocked.Exchange(ref _navigationInProgress, 0);
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
