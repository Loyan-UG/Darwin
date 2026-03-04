using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Consumer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Code-behind for the Discover page.
/// </summary>
/// <remarks>
/// This page hosts two discover journeys:
/// - Joined businesses: quick actions (Open QR / Open Rewards) for existing memberships.
/// - Explore businesses: search + navigation to detail for join or inspection flow.
///
/// It also maintains the Explore map surface by projecting Explore list items to map pins.
/// </remarks>
public partial class DiscoverPage : ContentPage
{
    private readonly DiscoverViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Pin, DiscoverExploreItem> _pinLookup = new();

    public DiscoverPage(DiscoverViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        BindingContext = _viewModel;

        // Keep map pins synchronized with current explore items.
        _viewModel.ExploreBusinesses.CollectionChanged += OnExploreBusinessesChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
        RebuildExploreMapPins();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Reset any selection state that could remain when leaving the page.
        ExploreBusinessesCollectionView.SelectedItem = null;
    }

    /// <summary>
    /// Selecting a joined business opens detail for quick context view.
    /// Dedicated action buttons in the same card provide direct QR/Rewards navigation.
    /// </summary>
    private async void OnJoinedBusinessSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0)
        {
            return;
        }

        try
        {
            if (e.CurrentSelection[0] is LoyaltyAccountSummary selected)
            {
                var detailsPage = _serviceProvider.GetRequiredService<BusinessDetailPage>();
                detailsPage.SetBusinessId(selected.BusinessId);
                await Navigation.PushAsync(detailsPage);
            }
        }
        finally
        {
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    /// <summary>
    /// Explore item behavior:
    /// - Joined business: open Rewards directly for faster redemption/accrual usage.
    /// - Not joined yet: open detail page to review and join flow.
    /// </summary>
    private async void OnExploreBusinessSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0)
        {
            return;
        }

        try
        {
            if (e.CurrentSelection[0] is DiscoverExploreItem selected)
            {
                await NavigateFromExploreSelectionAsync(selected);
            }
        }
        finally
        {
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    /// <summary>
    /// Quick action for opening QR tab in business context.
    /// </summary>
    private async void OnOpenQrClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Guid businessId } || businessId == Guid.Empty)
        {
            return;
        }

        await Shell.Current.GoToAsync($"//{Routes.Qr}", new System.Collections.Generic.Dictionary<string, object?>
        {
            ["businessId"] = businessId,
            ["joined"] = true
        });
    }

    /// <summary>
    /// Quick action for opening rewards tab in business context.
    /// </summary>
    private async void OnOpenRewardsClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Guid businessId } || businessId == Guid.Empty)
        {
            return;
        }

        await OpenRewardsAsync(businessId);
    }

    /// <summary>
    /// Triggered when Explore list changes and map pins must be refreshed.
    /// </summary>
    private void OnExploreBusinessesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildExploreMapPins();
    }

    /// <summary>
    /// Rebuilds map pins from Explore businesses with coordinates and recenters map.
    /// </summary>
    private void RebuildExploreMapPins()
    {
        _pinLookup.Clear();
        ExploreMap.Pins.Clear();

        Location? firstLocation = null;

        foreach (var item in _viewModel.ExploreBusinesses)
        {
            var coordinate = item.Business.Location;
            if (coordinate is null)
            {
                continue;
            }

            var pin = new Pin
            {
                Label = item.Business.Name,
                Address = item.Business.City ?? string.Empty,
                Type = PinType.Place,
                Location = new Location(coordinate.Latitude, coordinate.Longitude)
            };

            pin.MarkerClicked += OnExploreMapPinClicked;
            ExploreMap.Pins.Add(pin);
            _pinLookup[pin] = item;

            firstLocation ??= pin.Location;
        }

        if (firstLocation is not null)
        {
            // Keep map centered around loaded results. Radius is intentionally broad to include nearby pins.
            ExploreMap.MoveToRegion(MapSpan.FromCenterAndRadius(firstLocation, Distance.FromKilometers(2.5)));
        }
    }

    /// <summary>
    /// Opens the same destination used by list selection when a map marker is tapped.
    /// </summary>
    private async void OnExploreMapPinClicked(object? sender, PinClickedEventArgs e)
    {
        if (sender is not Pin pin || !_pinLookup.TryGetValue(pin, out var item))
        {
            return;
        }

        // Keep marker popup visible and navigate directly to detail/rewards flow.
        e.HideInfoWindow = false;
        await NavigateFromExploreSelectionAsync(item);
    }

    /// <summary>
    /// Centralized navigation policy for Explore entries to keep map/list behavior identical.
    /// </summary>
    private async Task NavigateFromExploreSelectionAsync(DiscoverExploreItem selected)
    {
        if (selected.IsJoined)
        {
            await OpenRewardsAsync(selected.BusinessId);
            return;
        }

        var detailsPage = _serviceProvider.GetRequiredService<BusinessDetailPage>();
        detailsPage.SetBusinessId(selected.BusinessId);
        await Navigation.PushAsync(detailsPage);
    }

    private static Task OpenRewardsAsync(Guid businessId)
    {
        return Shell.Current.GoToAsync($"//{Routes.Rewards}", new System.Collections.Generic.Dictionary<string, object?>
        {
            ["businessId"] = businessId
        });
    }
}
