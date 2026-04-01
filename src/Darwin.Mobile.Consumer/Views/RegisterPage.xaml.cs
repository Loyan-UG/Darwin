using System;
using Darwin.Mobile.Consumer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Consumer self-registration page.
/// </summary>
public partial class RegisterPage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;

    public RegisterPage(RegisterViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        NavigationPage.SetHasNavigationBar(this, false);
    }

    private async void OnLegalHubClicked(object? sender, EventArgs e)
    {
        var page = _serviceProvider.GetRequiredService<LegalHubPage>();
        await Navigation.PushAsync(page);
    }
}
