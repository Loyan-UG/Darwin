using Darwin.Mobile.Consumer.Constants;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Settings hub page.
///
/// Why this page is intentionally code-behind driven:
/// - Shell creates tab-root pages during fragment lifecycle on Android.
/// - Keeping constructor parameterless and avoiding early DI usage reduces null-race risk
///   in ShellSectionRenderer.OnCreateView.
/// - Navigation actions are dispatched on main thread explicitly for safety.
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

    /// <summary>
    /// Navigates through Shell on the UI thread with defensive guards.
    /// This avoids race conditions between Android fragment creation and Shell tree mutations.
    /// </summary>
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
                // Non-fatal guard: we log and keep app alive instead of letting fragment lifecycle crash.
                System.Diagnostics.Debug.WriteLine($"Settings navigation to '{route}' failed: {ex}");
            }
        });
    }
}
