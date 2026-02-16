using System;
using Darwin.Contracts.Businesses;
using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Consumer.ViewModels;
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

    public DiscoverPage(DiscoverViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
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
    /// Navigates to the business detail route and resets selection.
    /// </summary>
    private async void OnBusinessSelected(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.CurrentSelection.Count == 0)
            {
                return;
            }

            if (e.CurrentSelection[0] is BusinessSummary selected)
            {
                // Navigate to the dynamic route and pass business id in the route segment.
                await Shell.Current.GoToAsync($"{Routes.BusinessDetail}/{selected.Id}");

                // Clear selection to allow selecting the same item again later.
                ((CollectionView)sender).SelectedItem = null;
            }
        }
        catch
        {
            // Selection/navigation exceptions should not crash the page.
            // TODO: A centralized logger can be added here later if needed.
        }
    }
}
