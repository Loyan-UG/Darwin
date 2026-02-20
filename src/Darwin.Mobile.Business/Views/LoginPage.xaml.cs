using System;
using Darwin.Mobile.Business.ViewModels;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Code-behind for Business login page.
///
/// UX goals implemented here:
/// - Keep login as a focused standalone entry page.
/// - Dismiss keyboard before submit to avoid hidden error text.
/// - Auto-scroll to the top error area when a login/validation error appears.
/// </summary>
public partial class LoginPage : ContentPage
{

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;

        // Keep login isolated from app navigation chrome.
        Shell.SetNavBarIsVisible(this, false);
        Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);

        // Subscribe to VM event so the page can reveal the error block.
        viewModel.ErrorBecameVisibleRequested += OnErrorBecameVisibleRequested;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

#pragma warning disable CS4014
        (BindingContext as LoginViewModel)?.OnAppearingAsync();
#pragma warning restore CS4014
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Defensive unsubscribe to avoid duplicate handlers if page instance is recreated.
        if (BindingContext is LoginViewModel vm)
        {
            vm.ErrorBecameVisibleRequested -= OnErrorBecameVisibleRequested;
        }
    }

    /// <summary>
    /// Called when sign-in button is tapped.
    /// We explicitly remove focus from input controls so the keyboard closes
    /// and the user can immediately see any resulting error state.
    /// </summary>
    private void OnSignInClicked(object? sender, EventArgs e)
    {
        EmailEntry?.Unfocus();
        PasswordEntry?.Unfocus();
    }

    /// <summary>
    /// Ensures the error area is visible after an error is produced.
    /// </summary>
    private async void OnErrorBecameVisibleRequested()
    {
        try
        {
            if (LoginScrollView is not null)
            {
                // Small delay gives layout a chance to render the error block before scrolling.
                await Task.Delay(40);
                await LoginScrollView.ScrollToAsync(0, 0, true);
            }
        }
        catch
        {
            // I intentionally swallow UI scrolling exceptions because failing to scroll
            // must never block the login flow or crash the app.
        }
    }
}
