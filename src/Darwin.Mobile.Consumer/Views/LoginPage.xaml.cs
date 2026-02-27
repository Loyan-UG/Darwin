using Darwin.Mobile.Consumer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Code-behind for the login page.
/// </summary>
public partial class LoginPage
{
    private readonly IServiceProvider _serviceProvider;

    public LoginPage(LoginViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        // Set the binding context to the injected view model.
        BindingContext = viewModel;

        // Make this login page standalone:
        // - Hide the top navigation bar.
        // - Disable flyout menu to keep unauthenticated UX focused.
        Shell.SetNavBarIsVisible(this, false);
        Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);

        // Subscribe so the view model can request visibility for top-level validation/network errors.
        viewModel.ErrorBecameVisibleRequested += OnErrorBecameVisibleRequested;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // If ViewModel needs OnAppearingAsync, call it in fire-and-forget mode.
#pragma warning disable CS4014
        (BindingContext as LoginViewModel)?.OnAppearingAsync();
#pragma warning restore CS4014
    }

    /// <summary>
    /// Unsubscribes event handlers to avoid duplicate subscriptions on recreated pages.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is LoginViewModel vm)
        {
            vm.ErrorBecameVisibleRequested -= OnErrorBecameVisibleRequested;
        }
    }

    /// <summary>
    /// Scrolls the page to reveal error feedback when keyboard/screen size may hide it.
    /// </summary>
    private async void OnErrorBecameVisibleRequested()
    {
        try
        {
            if (RootScrollView is null)
            {
                return;
            }

            await Task.Delay(40);
            await RootScrollView.ScrollToAsync(0, 0, true);
        }
        catch
        {
            // Intentionally ignore animation/scroll exceptions so login flow never crashes.
        }
    }

    /// <summary>
    /// Navigates from Login to Register page.
    /// </summary>
    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        try
        {
            var page = _serviceProvider.GetService<RegisterPage>()
                ?? throw new InvalidOperationException("Register page is not registered in DI.");

            await Navigation.PushAsync(page);
        }
        catch
        {
            // Intentionally suppressed to avoid hard crash on edge navigation failure.
        }
    }

    /// <summary>
    /// Navigates from Login to ForgotPassword page.
    /// </summary>
    private async void OnForgotPasswordClicked(object? sender, EventArgs e)
    {
        try
        {
            var page = _serviceProvider.GetService<ForgotPasswordPage>()
                ?? throw new InvalidOperationException("Forgot password page is not registered in DI.");

            await Navigation.PushAsync(page);
        }
        catch
        {
            // Intentionally suppressed to avoid hard crash on edge navigation failure.
        }
    }
}
