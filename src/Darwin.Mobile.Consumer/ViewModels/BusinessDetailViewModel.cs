using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Loyalty;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// View model for displaying the details of a selected business and allowing the user
/// to join its loyalty program. After joining, a scan session is prepared and the user is
/// redirected to the QR page.
/// </summary>
public sealed class BusinessDetailViewModel : BaseViewModel
{
    private readonly IBusinessService _businessService;
    private readonly ILoyaltyService _loyaltyService;
    private readonly INavigationService _navigationService;

    private BusinessSummary? _business;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessDetailViewModel"/> class.
    /// </summary>
    /// <param name="businessService">Service to load business information.</param>
    /// <param name="loyaltyService">Service used to manage loyalty accounts and sessions.</param>
    /// <param name="navigationService">Navigation service used for page transitions.</param>
    public BusinessDetailViewModel(
        IBusinessService businessService,
        ILoyaltyService loyaltyService,
        INavigationService navigationService)
    {
        _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        JoinCommand = new AsyncRelayCommand(JoinLoyaltyProgramAsync, () => !IsBusy && BusinessId != Guid.Empty);
    }

    /// <summary>
    /// Gets the ID of the selected business. This must be set before loading the page.
    /// </summary>
    public Guid BusinessId { get; private set; }

    /// <summary>
    /// Gets the loaded business summary.
    /// </summary>
    public BusinessSummary? Business
    {
        get => _business;
        private set => SetProperty(ref _business, value);
    }

    /// <summary>
    /// Command triggered when the user chooses to join the business's loyalty program.
    /// </summary>
    public IAsyncRelayCommand JoinCommand { get; }

    /// <summary>
    /// Loads the business details when the page appears.
    /// </summary>
    public override async Task OnAppearingAsync()
    {
        if (BusinessId == Guid.Empty)
        {
            ErrorMessage = "Business not specified.";
            return;
        }

        if (Business is not null)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            // Load the full business details. Here we fetch the summary again; in a real app
            // you might call GetBusinessDetailAsync instead.
            var request = new BusinessListRequest { Page = 1, PageSize = 1, BusinessId = BusinessId };
            var response = await _businessService.ListAsync(request, CancellationToken.None)
                                                 .ConfigureAwait(false);

            Business = response?.Items?.Count > 0 ? response.Items[0] : null;

            if (Business is null)
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
    /// Sets the business context for this view model.
    /// </summary>
    /// <param name="businessId">The identifier of the business to display.</param>
    public void SetBusiness(Guid businessId)
    {
        BusinessId = businessId;
    }

    private async Task JoinLoyaltyProgramAsync()
    {
        if (BusinessId == Guid.Empty)
        {
            ErrorMessage = "Invalid business.";
            return;
        }

        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            // Attempt to create or retrieve the loyalty account for the current business.
            var joinResult = await _loyaltyService.CreateLoyaltyAccountAsync(BusinessId, CancellationToken.None)
                                                  .ConfigureAwait(false);

            if (!joinResult.Succeeded || joinResult.Value is null)
            {
                ErrorMessage = joinResult.Error ?? "Failed to join loyalty program.";
                return;
            }

            // After joining, prepare an accrual scan session.
            var sessionResult = await _loyaltyService.PrepareScanSessionAsync(
                    BusinessId,
                    LoyaltyScanMode.Accrual,
                    null,
                    CancellationToken.None)
                .ConfigureAwait(false);

            if (!sessionResult.Succeeded || sessionResult.Value is null)
            {
                ErrorMessage = sessionResult.Error ?? "Failed to create scan session.";
                return;
            }

            // Navigate to the QR tab, resetting the navigation stack.
            await _navigationService.GoToAsync($"//{Routes.Qr}").ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
