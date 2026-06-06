using System;
using System.Collections.Generic;
using System.Threading;
using Darwin.Mobile.Consumer.Services.Navigation;
using Darwin.Mobile.Consumer.ViewModels;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Page that completes the reset-password flow by accepting email, token, and a new password.
/// </summary>
public partial class ResetPasswordPage : ContentPage, IQueryAttributable
{
    private readonly IAppRootNavigator _rootNavigator;
    private readonly ResetPasswordViewModel _viewModel;
    private int _navigationInProgress;

    public ResetPasswordPage(ResetPasswordViewModel viewModel, IAppRootNavigator rootNavigator)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _rootNavigator = rootNavigator ?? throw new ArgumentNullException(nameof(rootNavigator));

        // Injected view model keeps navigation and business logic outside of code-behind.
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
            // Disappearing cleanup should never crash navigation away from password reset.
        }
        finally
        {
            base.OnDisappearing();
        }
    }

    /// <summary>
    /// Accepts optional recovery context from Shell/app-link style query parameters.
    /// Supports stable aliases so future email/deep-link handoff can prefill the same page
    /// without depending on one exact query-name shape.
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BindingContext is not ResetPasswordViewModel viewModel)
        {
            return;
        }

        var email = ReadQueryValue(query, "email");
        var token = ReadQueryValue(query, "token")
            ?? ReadQueryValue(query, "resetToken")
            ?? ReadQueryValue(query, "code");

        viewModel.ApplyPrefill(email, token);
    }

    private static string? ReadQueryValue(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var value))
        {
            return null;
        }

        var raw = value switch
        {
            string s => s,
            _ => value?.ToString()
        };

        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return SafeUnescape(raw);
    }

    /// <summary>
    /// Decodes query values defensively so malformed external recovery links cannot crash the reset page.
    /// </summary>
    /// <param name="raw">Raw query value supplied by Shell or an app link.</param>
    /// <returns>Decoded and trimmed value, or the trimmed raw value when decoding is not possible.</returns>
    private static string SafeUnescape(string raw)
    {
        try
        {
            return Uri.UnescapeDataString(raw).Trim();
        }
        catch (UriFormatException)
        {
            return raw.Trim();
        }
    }

    private async void OnReturnToLoginClicked(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _navigationInProgress, 1) == 1)
        {
            return;
        }

        try
        {
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopToRootAsync();
                return;
            }

            await _rootNavigator.NavigateToLoginAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Reset password return-to-login navigation failed: {ex}");
        }
        finally
        {
            Interlocked.Exchange(ref _navigationInProgress, 0);
        }
    }
}
