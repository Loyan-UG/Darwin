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
        if (!query.TryGetValue("businessId", out var value))
        {
            return;
        }

        if (value is Guid businessId && businessId != Guid.Empty)
        {
            _viewModel.SetBusiness(businessId);
            return;
        }

        var idString = value as string ?? value?.ToString();
        if (!string.IsNullOrWhiteSpace(idString) &&
            Guid.TryParse(SafeUnescape(idString), out var parsedBusinessId) &&
            parsedBusinessId != Guid.Empty)
        {
            _viewModel.SetBusiness(parsedBusinessId);
        }
    }

    /// <summary>
    /// Decodes route values defensively so malformed navigation input cannot crash business details.
    /// </summary>
    /// <param name="raw">Raw route value supplied by Shell.</param>
    /// <returns>Decoded and trimmed value, or the trimmed raw value when decoding is not possible.</returns>
    private static string SafeUnescape(string raw)
    {
        try
        {
            return Uri.UnescapeDataString(raw).Trim();
        }
        catch (UriFormatException)
        {
            return raw.Trim();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _viewModel.OnAppearingAsync();
        }
        catch
        {
            // Appearing is an async-void MAUI lifecycle hook. Detail load failures stay inside ViewModel feedback.
        }
    }

    /// <inheritdoc />
    protected override async void OnDisappearing()
    {
        try
        {
            await _viewModel.OnDisappearingAsync();
        }
        catch
        {
            // Disappearing cleanup should never crash navigation away from business detail.
        }
        finally
        {
            base.OnDisappearing();
        }
    }
}
