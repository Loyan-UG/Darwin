using System;
using System.Collections.Generic;
using Darwin.Mobile.Consumer.ViewModels;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Rewards page code-behind.
/// </summary>
/// <remarks>
/// Navigation contract:
/// - Accepts optional <c>businessId</c> query parameter.
/// - When provided, it preselects rewards context for that business before first load.
/// </remarks>
public partial class RewardsPage : IQueryAttributable
{
    private readonly RewardsViewModel _viewModel;

    public RewardsPage(RewardsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("businessId", out var rawBusinessId))
        {
            return;
        }

        if (rawBusinessId is Guid businessId && businessId != Guid.Empty)
        {
            _viewModel.SetBusiness(businessId);
            return;
        }

        if (rawBusinessId is string businessIdText &&
            Guid.TryParse(SafeUnescape(businessIdText), out var parsedBusinessId) &&
            parsedBusinessId != Guid.Empty)
        {
            _viewModel.SetBusiness(parsedBusinessId);
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
            // Appearing is an async-void MAUI lifecycle hook. Rewards load failures stay inside ViewModel feedback.
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
            // Disappearing cleanup should never crash navigation away from rewards.
        }
        finally
        {
            base.OnDisappearing();
        }
    }

    /// <summary>
    /// Decodes route values defensively so malformed navigation input cannot crash rewards context setup.
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
}
