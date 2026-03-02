using System;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Consumer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Code-behind for the Discover page.
/// </summary>
/// <remarks>
/// This page hosts two discover journeys:
/// - Joined businesses: quick actions (Open QR / Open Rewards) for existing memberships.
/// - Explore businesses: search + navigation to detail for join or inspection flow.
/// </remarks>
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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
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
                if (selected.IsJoined)
                {
                    await OpenRewardsAsync(selected.BusinessId);
                }
                else
                {
                    var detailsPage = _serviceProvider.GetRequiredService<BusinessDetailPage>();
                    detailsPage.SetBusinessId(selected.BusinessId);
                    await Navigation.PushAsync(detailsPage);
                }
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

    private static Task OpenRewardsAsync(Guid businessId)
    {
        return Shell.Current.GoToAsync($"//{Routes.Rewards}", new System.Collections.Generic.Dictionary<string, object?>
        {
            ["businessId"] = businessId
        });
    }
}