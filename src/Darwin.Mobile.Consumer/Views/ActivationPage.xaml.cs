using System;
using System.Collections.Generic;
using Darwin.Mobile.Consumer.ViewModels;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Page that supports requesting another activation email or confirming an account with email + token.
/// </summary>
public partial class ActivationPage : ContentPage, IQueryAttributable
{
    private readonly ActivationViewModel _viewModel;

    public ActivationPage(ActivationViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
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
            // Disappearing cleanup should never crash navigation away from activation.
        }
        finally
        {
            base.OnDisappearing();
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BindingContext is not ActivationViewModel viewModel)
        {
            return;
        }

        var email = ReadQueryValue(query, "email");
        var token = ReadQueryValue(query, "token")
            ?? ReadQueryValue(query, "confirmationToken")
            ?? ReadQueryValue(query, "confirmToken")
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
    /// Decodes query values defensively so malformed external links cannot crash the activation page.
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
}
