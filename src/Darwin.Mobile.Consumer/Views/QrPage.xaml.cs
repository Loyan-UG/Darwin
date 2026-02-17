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
/// - Accepts optional `businessName` and `joined` parameters for user context messaging.
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
        if (query.TryGetValue("businessId", out var rawValue)
            && rawValue is string businessIdText
            && Guid.TryParse(businessIdText, out var businessId))
        {
            _viewModel.SetBusiness(businessId);
        }

        if (query.TryGetValue("businessName", out var rawBusinessName)
            && rawBusinessName is string businessNameText)
        {
            _viewModel.SetBusinessDisplayName(Uri.UnescapeDataString(businessNameText));
        }

        var joined = false;
        if (query.TryGetValue("joined", out var rawJoined) && rawJoined is string joinedText)
        {
            joined = string.Equals(joinedText, "true", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(joinedText, "1", StringComparison.OrdinalIgnoreCase);
        }

        _viewModel.SetJoinedStatus(joined);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }
}
