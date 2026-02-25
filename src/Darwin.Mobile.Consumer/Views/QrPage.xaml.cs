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
        if (query.TryGetValue("businessId", out var rawBusinessId))
        {
            // Shell may pass query values either as typed objects (Guid) or as strings,
            // depending on how navigation parameters were supplied.
            if (rawBusinessId is Guid businessId)
            {
                _viewModel.SetBusiness(businessId);
            }
            else if (rawBusinessId is string businessIdText && Guid.TryParse(businessIdText, out var parsedBusinessId))
            {
                _viewModel.SetBusiness(parsedBusinessId);
            }
        }

        if (query.TryGetValue("businessName", out var rawBusinessName))
        {
            // Keep business name binding resilient for both plain values and encoded string values.
            var businessName = rawBusinessName as string;
            if (!string.IsNullOrWhiteSpace(businessName))
            {
                _viewModel.SetBusinessDisplayName(Uri.UnescapeDataString(businessName));
            }
        }

        var joined = false;
        if (query.TryGetValue("joined", out var rawJoined))
        {
            // Accept both typed bool and string values to make navigation robust across callers.
            joined = rawJoined switch
            {
                bool b => b,
                string joinedText => string.Equals(joinedText, "true", StringComparison.OrdinalIgnoreCase)
                                   || string.Equals(joinedText, "1", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        _viewModel.SetJoinedStatus(joined);

        // Trigger immediate first-load session creation after navigation parameters are applied.
        // This prevents a blank QR state when navigation timing causes OnAppearing to run earlier.
        _ = _viewModel.OnAppearingAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.OnDisappearingAsync();
    }
}
