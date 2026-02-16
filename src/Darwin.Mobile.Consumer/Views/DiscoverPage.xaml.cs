using System;
using Darwin.Contracts.Businesses;
using Darwin.Mobile.Consumer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Code-behind for the Discover page.
///
/// Responsibilities:
/// - Attach the injected <see cref="DiscoverViewModel"/> as BindingContext.
/// - Trigger initial data load when the page appears.
/// - Handle item selection and navigate to business details.
/// </summary>
public partial class DiscoverPage : ContentPage
{
    private readonly DiscoverViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public DiscoverPage(DiscoverViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        BindingContext = _viewModel;
    }

    /// <summary>
    /// Loads discovery data when the page becomes visible.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }

    /// <summary>
    /// Handles selection of a business from the collection view.
    /// Navigates to business details and resets selection.
    /// </summary>
    private async void OnBusinessSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0)
        {
            return;
        }

        try
        {
            if (e.CurrentSelection[0] is BusinessSummary selected)
            {
                // Resolve details page from DI so its constructor dependencies are satisfied.
                var detailsPage = _serviceProvider.GetRequiredService<BusinessDetailPage>();

                // Set context explicitly before pushing, so the page can load immediately.
                detailsPage.SetBusinessId(selected.Id);

                await Navigation.PushAsync(detailsPage);
            }
        }
        finally
        {
            // Always clear selection to allow selecting the same row again later.
            ((CollectionView)sender).SelectedItem = null;
        }
    }
}
