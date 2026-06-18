using System;
using System.Threading;
using Darwin.Mobile.Business.ViewModels;
using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Settings hub page code-behind for Business app.
/// </summary>
public partial class SettingsPage : ContentPage
{
    private static readonly TimeSpan LogoutTimeout = TimeSpan.FromSeconds(10);
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _viewModel.OnAppearingAsync();
        }
        catch
        {
            // Settings status load failures stay inside ViewModel feedback.
        }
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            AppResources.LogoutConfirmTitle,
            AppResources.LogoutConfirmMessage,
            AppResources.LogoutButtonText,
            AppResources.LogoutConfirmCancel);

        if (!confirmed)
        {
            return;
        }

        try
        {
            var auth = Handler?.MauiContext?.Services?.GetService<IAuthService>();
            if (auth is not null)
            {
                using var timeout = new CancellationTokenSource(LogoutTimeout);
                await auth.LogoutAsync(timeout.Token);
            }
        }
        finally
        {
            await Shell.Current.GoToAsync($"//{Routes.Login}");
        }
    }
}
