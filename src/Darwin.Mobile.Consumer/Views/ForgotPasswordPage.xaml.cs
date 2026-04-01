using System;
using Darwin.Mobile.Consumer.ViewModels;
using Microsoft.Extensions.DependencyInjection;


namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Forgot-password page that allows the user to request password reset instructions.
/// </summary>
public partial class ForgotPasswordPage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;

    public ForgotPasswordPage(ForgotPasswordViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        // Injected view model keeps this page lean and consistent with DI-first architecture.
        BindingContext = viewModel;
        NavigationPage.SetHasNavigationBar(this, false);
    }

    /// <summary>
    /// Opens the reset-password completion page where the user can submit email, token and new password.
    /// </summary>
    private async void OnGoToResetPasswordClicked(object? sender, EventArgs e)
    {
        try
        {
            var page = _serviceProvider.GetService<ResetPasswordPage>()
                ?? throw new InvalidOperationException("Reset password page is not registered in DI.");

            await Navigation.PushAsync(page);
        }
        catch
        {
            // Intentionally suppressed to avoid hard crash on edge navigation failure.
        }
    }
}
