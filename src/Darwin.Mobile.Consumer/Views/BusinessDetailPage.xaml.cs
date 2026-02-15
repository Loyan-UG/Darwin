using System;
using System.Collections.Generic;
using Darwin.Mobile.Consumer.ViewModels;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Code-behind for BusinessDetailPage. Implements <see cref="IQueryAttributable"/>
/// to receive the business ID from the navigation route.
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
}
