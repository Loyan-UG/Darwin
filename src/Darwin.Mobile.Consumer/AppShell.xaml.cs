using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Consumer.Views;
using Darwin.Mobile.Shared.Services;
using Microsoft.Maui.Controls;
using System;
using System.Threading;

namespace Darwin.Mobile.Consumer;

/// <summary>
/// Code-behind for the main shell. Registers dynamic routes and handles logout.
/// </summary>
public partial class AppShell : Shell
{
    private readonly IAuthService _authService;

    public AppShell(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        Routing.RegisterRoute($"{Routes.BusinessDetail}/{{businessId}}", typeof(BusinessDetailPage));
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        try
        {
            await _authService.LogoutAsync(CancellationToken.None);
        }
        finally
        {
            // Route through App so root replacement remains centralized and window-aware.
            if (Application.Current is App app)
            {
                await app.NavigateToLoginAsync();
            }
        }
    }
}
