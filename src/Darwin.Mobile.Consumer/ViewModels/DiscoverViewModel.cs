using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Businesses;
using Darwin.Mobile.Shared.ViewModels;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Commands;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// View model for the Discover tab in the consumer app.
/// Fetches and exposes a list of businesses for display.
/// </summary>
public sealed class DiscoverViewModel : BaseViewModel
{
    private readonly IBusinessService _businessService;
    private bool _hasLoaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoverViewModel"/> class.
    /// </summary>
    /// <param name="businessService">Service used to retrieve business listings.</param>
    public DiscoverViewModel(IBusinessService businessService)
    {
        _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        Businesses = new ObservableCollection<BusinessSummary>();
        RefreshCommand = new AsyncCommand(RefreshAsync);
    }

    /// <summary>
    /// Gets the collection of businesses displayed in the view.
    /// </summary>
    public ObservableCollection<BusinessSummary> Businesses { get; }

    /// <summary>
    /// Command used to refresh the business list.
    /// </summary>
    public AsyncCommand RefreshCommand { get; }

    /// <summary>
    /// Called when the page appears. Initiates a refresh on first load.
    /// </summary>
    public override async Task OnAppearingAsync()
    {
        if (!_hasLoaded)
        {
            await RefreshAsync().ConfigureAwait(false);
            _hasLoaded = true;
        }
    }

    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var request = new BusinessListRequest
            {
                Page = 1,
                PageSize = 50
            };

            var response = await _businessService
                .ListAsync(request, CancellationToken.None)
                .ConfigureAwait(false);

            Businesses.Clear();

            if (response?.Items != null)
            {
                foreach (var business in response.Items.OrderBy(b => b.Name))
                {
                    Businesses.Add(business);
                }
            }
            else
            {
                ErrorMessage = "Failed to load businesses.";
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
}
