using System;
using System.Collections.Generic;
using Darwin.Mobile.Consumer.ViewModels;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Code-behind for BusinessDetailPage.
/// Supports both Shell query-based initialization and direct page initialization.
/// </summary>
public partial class BusinessDetailPage : ContentPage, IQueryAttributable
{
    private readonly BusinessDetailViewModel _viewModel;

    public BusinessDetailPage(BusinessDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
    }

    /// <summary>
    /// Allows callers (for example DiscoverPage) to initialize business context before push navigation.
    /// </summary>
    public void SetBusinessId(Guid businessId)
    {
        _viewModel.SetBusiness(businessId);
    }

    /// <summary>
    /// Receives query parameters after navigation and sets the business ID
    /// on the view model.
    /// </summary>
    /// <param name="query">Dictionary of query parameters.</param>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("businessId", out var value) &&
            value is string idString &&
            Guid.TryParse(idString, out var id))
        {
            _viewModel.SetBusiness(id);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }
}
