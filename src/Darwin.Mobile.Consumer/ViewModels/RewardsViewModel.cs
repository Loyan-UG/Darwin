using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Shared.ViewModels;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Commands;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Displays the current points balance and available rewards.
/// </summary>
public sealed class RewardsViewModel : BaseViewModel
{
    private readonly ILoyaltyService _loyaltyService;
    private Guid _businessId;
    private bool _loaded;
    private int _pointsBalance;

    public RewardsViewModel(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        AvailableRewards = new ObservableCollection<LoyaltyRewardSummary>();
        RefreshCommand = new AsyncCommand(RefreshAsync);
    }

    public Guid BusinessId
    {
        get => _businessId;
        private set => SetProperty(ref _businessId, value);
    }

    public int PointsBalance
    {
        get => _pointsBalance;
        private set => SetProperty(ref _pointsBalance, value);
    }

    public ObservableCollection<LoyaltyRewardSummary> AvailableRewards { get; }

    // Backwards-compatibility: alias 'Rewards' maps to same collection.
    public ObservableCollection<LoyaltyRewardSummary> Rewards => AvailableRewards;

    public AsyncCommand RefreshCommand { get; }

    public void SetBusiness(Guid businessId)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("Business id must not be empty.", nameof(businessId));

        BusinessId = businessId;
    }

    public override async Task OnAppearingAsync()
    {
        if (!_loaded && BusinessId != Guid.Empty)
        {
            await RefreshAsync().ConfigureAwait(false);
            _loaded = true;
        }
    }

    private async Task RefreshAsync()
    {
        if (BusinessId == Guid.Empty)
        {
            ErrorMessage = "Business context is not set.";
            return;
        }

        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var accountResult = await _loyaltyService.GetAccountSummaryAsync(BusinessId, CancellationToken.None)
                                                    .ConfigureAwait(false);

            if (accountResult?.Value == null)
            {
                ErrorMessage = accountResult?.Error ?? "Failed to load loyalty account.";
                return;
            }

            PointsBalance = accountResult.Value.PointsBalance;

            var rewardsResult = await _loyaltyService.GetAvailableRewardsAsync(BusinessId, CancellationToken.None)
                                                    .ConfigureAwait(false);

            AvailableRewards.Clear();

            if (rewardsResult?.Value != null)
            {
                foreach (var reward in rewardsResult.Value.OrderBy(r => r.RequiredPoints))
                {
                    AvailableRewards.Add(reward);
                }
            }
            else
            {
                ErrorMessage = rewardsResult?.Error ?? "Failed to load rewards.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
