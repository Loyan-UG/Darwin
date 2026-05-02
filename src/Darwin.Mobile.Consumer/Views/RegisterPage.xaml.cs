using System;
using System.Linq;
using System.Threading;
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
    private readonly RegisterViewModel _viewModel;
    private int _navigationInProgress;

    public RegisterPage(RegisterViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        NavigationPage.SetHasNavigationBar(this, false);
    }

    /// <inheritdoc />
    protected override async void OnDisappearing()
    {
        try
        {
            await _viewModel.OnDisappearingAsync();
        }
        catch
        {
            // Disappearing cleanup should never crash navigation away from registration.
        }
        finally
        {
            base.OnDisappearing();
        }
    }

    private async void OnLegalHubClicked(object? sender, EventArgs e)
    {
        await PushPageSafelyAsync<LegalHubPage>();
    }

    private async void OnReturnToLoginClicked(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _navigationInProgress, 1) == 1)
        {
            return;
        }

        if (BindingContext is not RegisterViewModel registerViewModel)
        {
            Interlocked.Exchange(ref _navigationInProgress, 0);
            return;
        }

        try
        {
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Register return-to-login navigation failed: {ex}");
        }
        finally
        {
            Interlocked.Exchange(ref _navigationInProgress, 0);
        }
    }

    private async void OnOpenActivationClicked(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _navigationInProgress, 1) == 1)
        {
            return;
        }

        if (BindingContext is not RegisterViewModel registerViewModel)
        {
            Interlocked.Exchange(ref _navigationInProgress, 0);
            return;
        }

        try
        {
            var activationPage = _serviceProvider.GetRequiredService<ActivationPage>();
            if (activationPage.BindingContext is ActivationViewModel activationViewModel)
            {
                activationViewModel.ApplyPrefill(registerViewModel.Email);
            }

            await Navigation.PushAsync(activationPage);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Register activation navigation failed: {ex}");
        }
        finally
        {
            Interlocked.Exchange(ref _navigationInProgress, 0);
        }
    }

    private async Task PushPageSafelyAsync<TPage>() where TPage : Page
    {
        if (Interlocked.Exchange(ref _navigationInProgress, 1) == 1)
        {
            return;
        }

        try
        {
            var page = _serviceProvider.GetRequiredService<TPage>();
            await Navigation.PushAsync(page);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Register navigation to '{typeof(TPage).Name}' failed: {ex}");
        }
        finally
        {
            Interlocked.Exchange(ref _navigationInProgress, 0);
        }
    }
}
