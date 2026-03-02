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
            Guid.TryParse(Uri.UnescapeDataString(businessIdText), out var parsedBusinessId) &&
            parsedBusinessId != Guid.Empty)
        {
            _viewModel.SetBusiness(parsedBusinessId);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }
}