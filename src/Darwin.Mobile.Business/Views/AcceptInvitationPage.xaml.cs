using Darwin.Mobile.Business.ViewModels;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Focused onboarding page for business invitation acceptance.
/// The page supports both token-entry and future query-based prefill via Shell.
/// </summary>
public partial class AcceptInvitationPage : ContentPage, IQueryAttributable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptInvitationPage"/> class.
    /// </summary>
    public AcceptInvitationPage(AcceptInvitationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);
    }

    /// <summary>
    /// Accepts optional invitation-token handoff from Shell/app-link style query parameters.
    /// Supports stable aliases so onboarding links do not depend on one exact query name.
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BindingContext is not AcceptInvitationViewModel viewModel)
        {
            return;
        }

        var token = ReadQueryValue(query, "token")
            ?? ReadQueryValue(query, "invitationToken")
            ?? ReadQueryValue(query, "invitation")
            ?? ReadQueryValue(query, "code");

        if (!string.IsNullOrWhiteSpace(token))
        {
            viewModel.InvitationToken = token;
        }
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ((AcceptInvitationViewModel)BindingContext).OnAppearingAsync();
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
