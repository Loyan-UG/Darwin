using Darwin.Mobile.Consumer.ViewModels;
using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Displays the rotating QR code for the consumer.
///
/// Navigation contract:
/// - Accepts a `businessId` query parameter (GUID).
/// - Sets the business context on the view model before OnAppearing refresh.
/// </summary>
public partial class QrPage : IQueryAttributable
{
    private readonly QrViewModel _viewModel;

    public QrPage(QrViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // The query dictionary contains string values when route params are passed via Shell URI.
        if (query.TryGetValue("businessId", out var rawValue)
            && rawValue is string businessIdText
            && Guid.TryParse(businessIdText, out var businessId))
        {
            _viewModel.SetBusiness(businessId);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }
}
