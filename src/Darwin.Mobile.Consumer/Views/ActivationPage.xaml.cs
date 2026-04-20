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
    public ActivationPage(ActivationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        NavigationPage.SetHasNavigationBar(this, false);
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

        return Uri.UnescapeDataString(raw).Trim();
    }
}
