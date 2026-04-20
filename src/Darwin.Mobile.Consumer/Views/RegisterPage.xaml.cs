using System;
using System.Linq;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Consumer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Consumer self-registration page.
/// </summary>
public partial class RegisterPage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;

    public RegisterPage(RegisterViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        NavigationPage.SetHasNavigationBar(this, false);
    }

    private async void OnLegalHubClicked(object? sender, EventArgs e)
    {
        var page = _serviceProvider.GetRequiredService<LegalHubPage>();
        await Navigation.PushAsync(page);
    }

    private async void OnReturnToLoginClicked(object? sender, EventArgs e)
    {
        if (BindingContext is not RegisterViewModel registerViewModel)
        {
            return;
        }

        var pendingActivation = registerViewModel.HasPendingEmailConfirmation;
        var infoMessage = pendingActivation ? AppResources.RegisterEmailConfirmationSent : null;

        var existingLoginPage = Navigation.NavigationStack
            .OfType<LoginPage>()
            .LastOrDefault();

        if (existingLoginPage?.BindingContext is LoginViewModel existingLoginViewModel)
        {
            existingLoginViewModel.ApplyEntryContext(registerViewModel.Email, infoMessage, pendingActivation);
            await Navigation.PopAsync();
            return;
        }

        var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
        if (loginPage.BindingContext is LoginViewModel loginViewModel)
        {
            loginViewModel.ApplyEntryContext(registerViewModel.Email, infoMessage, pendingActivation);
        }

        await Navigation.PushAsync(loginPage);
    }

    private async void OnOpenActivationClicked(object? sender, EventArgs e)
    {
        if (BindingContext is not RegisterViewModel registerViewModel)
        {
            return;
        }

        var activationPage = _serviceProvider.GetRequiredService<ActivationPage>();
        if (activationPage.BindingContext is ActivationViewModel activationViewModel)
        {
            activationViewModel.ApplyPrefill(registerViewModel.Email);
        }

        await Navigation.PushAsync(activationPage);
    }
}
