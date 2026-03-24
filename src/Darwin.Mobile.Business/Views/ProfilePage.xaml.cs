using System;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Business.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Business profile edit page code-behind.
/// </summary>
public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _viewModel;

    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }

    /// <summary>
    /// Opens the business account-deletion legal handoff screen from the profile page.
    /// </summary>
    private async void OnAccountDeletionClicked(object? sender, EventArgs e)
    {
        await MainThread.InvokeOnMainThreadAsync(() => NavigateSafelyAsync(Routes.SettingsAccountDeletion));
    }

    private static async Task NavigateSafelyAsync(string route)
    {
        if (Shell.Current is null || string.IsNullOrWhiteSpace(route))
        {
            return;
        }

        try
        {
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Business profile navigation to '{route}' failed: {ex}");
        }
    }
}
