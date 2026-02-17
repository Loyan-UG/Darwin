using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// View model for the Business Detail page.
/// Loads business details, allows the user to join the loyalty program,
/// prepares a scan session and navigates to the QR page.
/// </summary>
public sealed class BusinessDetailViewModel : BaseViewModel
{
    private readonly IBusinessService _businessService;
    private readonly ILoyaltyService _loyaltyService;
    private readonly INavigationService _navigationService;

    private BusinessDetail? _business;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessDetailViewModel"/> class.
    /// </summary>
    public BusinessDetailViewModel(
        IBusinessService businessService,
        ILoyaltyService loyaltyService,
        INavigationService navigationService)
    {
        _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        JoinCommand = new AsyncRelayCommand(JoinAsync, () => !IsBusy && BusinessId != Guid.Empty);
    }

    /// <summary>
    /// The ID of the business being viewed.
    /// </summary>
    public Guid BusinessId { get; private set; }

    /// <summary>
    /// The loaded business detail.
    /// </summary>
    public BusinessDetail? Business
    {
        get => _business;
        private set => SetProperty(ref _business, value);
    }

    /// <summary>
    /// Command triggered when the user chooses to join the loyalty program.
    /// </summary>
    public IAsyncRelayCommand JoinCommand { get; }

    /// <summary>
    /// Loads business details on first appearance.
    /// </summary>
    public override async Task OnAppearingAsync()
    {
        if (BusinessId == Guid.Empty || Business != null)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            // Retrieve business details by ID.
            Business = await _businessService.GetAsync(BusinessId, CancellationToken.None);

            if (Business == null)
            {
                ErrorMessage = "Business not found.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Sets the business context for the view model.
    /// </summary>
    /// <param name="businessId">Identifier of the business to load.</param>
    public void SetBusiness(Guid businessId)
    {
        BusinessId = businessId;
    }

    private async Task JoinAsync()
    {
        if (BusinessId == Guid.Empty || IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            // Join the loyalty program for this business.
            var joinResult = await _loyaltyService.JoinLoyaltyAsync(BusinessId, null, CancellationToken.None);

            if (!joinResult.Succeeded || joinResult.Value == null)
            {
                ErrorMessage = joinResult.Error ?? "Unable to join the loyalty program.";
                return;
            }

            // Prepare a scan session in accrual mode.
            var sessionResult = await _loyaltyService.PrepareScanSessionAsync(
                BusinessId,
                LoyaltyScanMode.Accrual,
                selectedRewardIds: null,
                CancellationToken.None);

            if (!sessionResult.Succeeded || sessionResult.Value == null)
            {
                ErrorMessage = sessionResult.Error ?? "Unable to create scan session.";
                return;
            }

            // Navigate to the QR tab and pass the active business context using Shell parameters.
            // Using a parameter dictionary avoids brittle URI construction and escaping issues.
            var parameters = new Dictionary<string, object?>
            {
                ["businessId"] = BusinessId,
                ["businessName"] = Business?.Name,
                ["joined"] = true
            };

            await _navigationService.GoToAsync($"//{Routes.Qr}", parameters);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
