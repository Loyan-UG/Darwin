using System;
using System.Threading;
using Darwin.Mobile.Consumer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Forgot-password page that allows the user to request password reset instructions.
/// </summary>
public partial class ForgotPasswordPage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ForgotPasswordViewModel _viewModel;
    private int _navigationInProgress;

    public ForgotPasswordPage(ForgotPasswordViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        // Injected view model keeps this page lean and consistent with DI-first architecture.
        BindingContext = _viewModel;
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
            // Disappearing cleanup should never crash navigation away from password recovery.
        }
        finally
        {
            base.OnDisappearing();
        }
    }

    /// <summary>
    /// Opens the reset-password completion page where the user can submit email, token and new password.
    /// </summary>
    private async void OnGoToResetPasswordClicked(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _navigationInProgress, 1) == 1)
        {
            return;
        }

        try
        {
            var page = _serviceProvider.GetService<ResetPasswordPage>()
                ?? throw new InvalidOperationException("Reset password page is not registered in DI.");

            if (page.BindingContext is ResetPasswordViewModel resetPasswordViewModel &&
                BindingContext is ForgotPasswordViewModel forgotPasswordViewModel)
            {
                resetPasswordViewModel.ApplyPrefill(forgotPasswordViewModel.Email);
            }

            await Navigation.PushAsync(page);
        }
        catch
        {
            // Intentionally suppressed to avoid hard crash on edge navigation failure.
        }
        finally
        {
            Interlocked.Exchange(ref _navigationInProgress, 0);
        }
    }
}
