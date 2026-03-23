using System;
using Darwin.Mobile.Business.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Business legal hub page that keeps navigation to the account deletion warning page available in both login and settings flows.
/// </summary>
public partial class LegalHubPage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;

    public LegalHubPage(LegalHubViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        BindingContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    private async void OnAccountDeletionClicked(object? sender, EventArgs e)
    {
        var page = _serviceProvider.GetRequiredService<AccountDeletionPage>();
        await Navigation.PushAsync(page);
    }
}
