using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
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
    private int _navigationInProgress;

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

        try
        {
            await _viewModel.OnAppearingAsync();
            RebuildExploreMapPins();
        }
        catch
        {
            // Appearing is an async-void MAUI lifecycle hook. Keep unexpected load/map failures from crashing the app.
        }
    }

    protected override async void OnDisappearing()
    {
        try
        {
            await _viewModel.OnDisappearingAsync();
        }
        catch
        {
            // Disappearing cleanup should never crash navigation away from Discover.
        }
        finally
        {
            base.OnDisappearing();
            // Reset any selection state that could remain when leaving the page.
            DiscoverCollectionView.SelectedItem = null;
        }
    }

    /// <summary>
    /// Routes the selected Discover row according to the active card type.
    /// </summary>
    /// <remarks>
    /// The page uses a single virtualized list for both tabs. Selection therefore inspects the display wrapper
    /// and dispatches to the same navigation policy that was previously split across two nested lists.
    /// </remarks>
    private async void OnDiscoverItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0)
        {
            return;
        }

        try
        {
            if (e.CurrentSelection[0] is not DiscoverDisplayItem selected)
            {
                return;
            }

            if (selected.JoinedAccount is not null)
            {
                await OpenBusinessDetailAsync(selected.JoinedAccount.BusinessId);
                return;
            }

            if (selected.ExploreItem is not null)
            {
                await NavigateFromExploreSelectionAsync(selected.ExploreItem);
            }
        }
        catch
        {
            // Selection navigation is best-effort; duplicate taps or stale navigation state must not crash Discover.
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
        var businessId = ResolveBusinessId(sender);
        if (businessId == Guid.Empty)
        {
            return;
        }

        try
        {
            await NavigateSafelyAsync(() => Shell.Current.GoToAsync($"//{Routes.Qr}", new Dictionary<string, object?>
            {
                ["businessId"] = businessId,
                ["joined"] = true
            }));
        }
        catch
        {
            // Quick-action navigation should fail closed and let the user tap again after Shell state recovers.
        }
    }

    /// <summary>
    /// Quick action for opening rewards tab in business context.
    /// </summary>
    private async void OnOpenRewardsClicked(object? sender, EventArgs e)
    {
        var businessId = ResolveBusinessId(sender);
        if (businessId == Guid.Empty)
        {
            return;
        }

        try
        {
            await OpenRewardsAsync(businessId);
        }
        catch
        {
            // Quick-action navigation should fail closed and let the user tap again after Shell state recovers.
        }
    }

    /// <summary>
    /// Triggered when Explore list changes and map pins must be refreshed.
    /// </summary>
    private void OnExploreBusinessesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        try
        {
            RebuildExploreMapPins();
        }
        catch
        {
            // Map rendering is supplementary; list content remains the source of truth if pins cannot be rebuilt.
        }
    }

    /// <summary>
    /// Rebuilds map pins from Explore businesses with coordinates and recenters map.
    /// </summary>
    private void RebuildExploreMapPins()
    {
        if (ExploreMap is null)
        {
            return;
        }

        DetachExploreMapPins();
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
        try
        {
            await NavigateFromExploreSelectionAsync(item);
        }
        catch
        {
            // Marker navigation follows the same best-effort policy as list navigation.
        }
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

        await OpenBusinessDetailAsync(selected.BusinessId);
    }

    private Task OpenRewardsAsync(Guid businessId)
    {
        return NavigateSafelyAsync(() => Shell.Current.GoToAsync($"//{Routes.Rewards}", new Dictionary<string, object?>
            {
                ["businessId"] = businessId
            }));
    }

    /// <summary>
    /// Opens a business detail page while preventing duplicate pushes from quick repeated taps.
    /// </summary>
    private Task OpenBusinessDetailAsync(Guid businessId)
    {
        return NavigateSafelyAsync(async () =>
        {
            var detailsPage = _serviceProvider.GetRequiredService<BusinessDetailPage>();
            detailsPage.SetBusinessId(businessId);
            await Navigation.PushAsync(detailsPage);
        });
    }

    /// <summary>
    /// Serializes code-behind navigation so map taps, list selections, and quick actions cannot overlap.
    /// </summary>
    private async Task NavigateSafelyAsync(Func<Task> navigate)
    {
        if (Interlocked.Exchange(ref _navigationInProgress, 1) == 1)
        {
            return;
        }

        try
        {
            await navigate();
        }
        finally
        {
            Interlocked.Exchange(ref _navigationInProgress, 0);
        }
    }

    /// <summary>
    /// Resolves business id from native MAUI and Syncfusion button senders.
    /// </summary>
    private static Guid ResolveBusinessId(object? sender)
    {
        return sender switch
        {
            Button { CommandParameter: Guid businessId } => businessId,
            Syncfusion.Maui.Toolkit.Buttons.SfButton { CommandParameter: Guid businessId } => businessId,
            _ => Guid.Empty
        };
    }

    /// <summary>
    /// Detaches marker callbacks before map pins are replaced so stale pin instances cannot call back into this page.
    /// </summary>
    private void DetachExploreMapPins()
    {
        foreach (var pin in _pinLookup.Keys)
        {
            pin.MarkerClicked -= OnExploreMapPinClicked;
        }
    }
}
