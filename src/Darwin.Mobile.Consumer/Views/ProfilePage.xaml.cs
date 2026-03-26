using System;
using System.Threading.Tasks;
using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Consumer.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Profile page code-behind.
/// Binds to <see cref="ProfileViewModel"/> and forwards lifecycle events.
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
    /// Opens the consumer account deletion request screen from the profile page.
    /// </summary>
    private async void OnAccountDeletionClicked(object? sender, EventArgs e)
    {
        await MainThread.InvokeOnMainThreadAsync(() => NavigateSafelyAsync(Routes.AccountDeletion));
    }

    /// <summary>
    /// Opens the member address-book management screen from the profile page.
    /// </summary>
    private async void OnManageAddressesClicked(object? sender, EventArgs e)
    {
        await MainThread.InvokeOnMainThreadAsync(() => NavigateSafelyAsync(Routes.MemberAddresses));
    }

    /// <summary>
    /// Opens the member preferences management screen from the profile page.
    /// </summary>
    private async void OnManagePreferencesClicked(object? sender, EventArgs e)
    {
        await MainThread.InvokeOnMainThreadAsync(() => NavigateSafelyAsync(Routes.MemberPreferences));
    }

    /// <summary>
    /// Opens the member CRM customer-context details screen from the profile page.
    /// </summary>
    private async void OnViewCustomerContextClicked(object? sender, EventArgs e)
    {
        await MainThread.InvokeOnMainThreadAsync(() => NavigateSafelyAsync(Routes.MemberCustomerContext));
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
            System.Diagnostics.Debug.WriteLine($"Profile navigation to '{route}' failed: {ex}");
        }
    }
}
