using System;
using Darwin.Contracts.Businesses;
using Darwin.Mobile.Consumer.Constants;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Code-behind for the DiscoverPage. Handles selection of a business in the list.
/// </summary>
public partial class DiscoverPage : ContentPage
{
    public DiscoverPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the selection of a business from the collection view.
    /// Navigates to the business detail route and resets the selection.
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
                // Navigate to the dynamic route, passing the business ID as a segment.
                await Shell.Current.GoToAsync($"{Routes.BusinessDetail}/{selected.Id}");

                // Reset the selection so the same item can be selected again later.
                ((CollectionView)sender).SelectedItem = null;
            }
        }
        catch
        {
            // Optionally log or show a message to the user.
        }
    }
}
