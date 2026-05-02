using System;
using System.Threading;
using Darwin.Mobile.Consumer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Consumer legal hub page that keeps navigation to the account deletion warning page available in both pre-login and post-login flows.
/// </summary>
public partial class LegalHubPage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LegalHubViewModel _viewModel;
    private int _navigationInProgress;

    public LegalHubPage(LegalHubViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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
            // Disappearing cleanup should never crash navigation away from legal hub.
        }
        finally
        {
            base.OnDisappearing();
        }
    }

    private async void OnAccountDeletionClicked(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _navigationInProgress, 1) == 1)
        {
            return;
        }

        try
        {
            var page = _serviceProvider.GetRequiredService<AccountDeletionPage>();
            await Navigation.PushAsync(page);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Consumer legal hub account deletion navigation failed: {ex}");
        }
        finally
        {
            Interlocked.Exchange(ref _navigationInProgress, 0);
        }
    }
}
