using Darwin.Mobile.Consumer.Constants;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Settings hub page.
/// </summary>
public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        await NavigateSafelyAsync(Routes.ProfileEdit);
    }

    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        await NavigateSafelyAsync(Routes.ChangePassword);
    }

    private async void OnMemberCommerceClicked(object sender, EventArgs e)
    {
        await NavigateSafelyAsync(Routes.MemberCommerce);
    }

    private async void OnLegalHubClicked(object sender, EventArgs e)
    {
        await NavigateSafelyAsync(Routes.LegalHub);
    }

    private async void OnAccountDeletionClicked(object sender, EventArgs e)
    {
        await NavigateSafelyAsync(Routes.AccountDeletion);
    }

    private static Task NavigateSafelyAsync(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return Task.CompletedTask;
        }

        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Shell.Current is null)
            {
                return;
            }

            try
            {
                await Shell.Current.GoToAsync(route);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings navigation to '{route}' failed: {ex}");
            }
        });
    }
}
