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
        BindingContext = viewModel;

        Shell.SetNavBarIsVisible(this, false);
        Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);

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

        if (BindingContext is LoginViewModel vm)
        {
            vm.ErrorBecameVisibleRequested -= OnErrorBecameVisibleRequested;
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

            await Task.Delay(40);
            await RootScrollView.ScrollToAsync(0, 0, true);
        }
        catch
        {
        }
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        await PushPageSafelyAsync<RegisterPage>();
    }

    private async void OnForgotPasswordClicked(object? sender, EventArgs e)
    {
        await PushPageSafelyAsync<ForgotPasswordPage>();
    }

    private async void OnLegalHubClicked(object? sender, EventArgs e)
    {
        await PushPageSafelyAsync<LegalHubPage>();
    }

    private void OnEmailEntryFocused(object? sender, FocusEventArgs e)
    {
        EmailBorder.Stroke = (Color)Application.Current!.Resources["BrandGold500"];
    }

    private void OnEmailEntryUnfocused(object? sender, FocusEventArgs e)
    {
        EmailBorder.Stroke = (Color)Application.Current!.Resources["Neutral100"];
    }

    private void OnPasswordEntryFocused(object? sender, FocusEventArgs e)
    {
        PasswordBorder.Stroke = (Color)Application.Current!.Resources["BrandGold500"];
    }

    private void OnPasswordEntryUnfocused(object? sender, FocusEventArgs e)
    {
        PasswordBorder.Stroke = (Color)Application.Current!.Resources["Neutral100"];
    }

    private async Task PushPageSafelyAsync<TPage>() where TPage : Page
    {
        try
        {
            var page = _serviceProvider.GetService<TPage>()
                ?? throw new InvalidOperationException($"{typeof(TPage).Name} is not registered in DI.");

            await Navigation.PushAsync(page);
        }
        catch
        {
        }
    }
}
