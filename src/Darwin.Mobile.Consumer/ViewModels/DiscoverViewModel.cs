using Darwin.Contracts.Businesses;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// View model for the discovery page. Loads a simple list of businesses from the API.
/// In future phases this will support map viewports, filtering and searching.
/// </summary>
public sealed class DiscoverViewModel : BaseViewModel
{
    private readonly IBusinessService _businessService;
    private bool _hasLoaded;

    public DiscoverViewModel(IBusinessService businessService)
    {
        _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        Businesses = new ObservableCollection<BusinessSummary>();
        RefreshCommand = new AsyncCommand(RefreshAsync);
    }

    /// <summary>
    /// Gets the collection of businesses retrieved from the server.
    /// </summary>
    public ObservableCollection<BusinessSummary> Businesses { get; }

    /// <summary>
    /// Command that refreshes the business list.
    /// </summary>
    public AsyncCommand RefreshCommand { get; }

    /// <inheritdoc />
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
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            // Prepare a minimal request: ask for the first page of businesses.
            var req = new BusinessListRequest
            {
                Page = 1,
                PageSize = 25
            };

            var result = await _businessService.ListAsync(req, CancellationToken.None).ConfigureAwait(false);
            if (result == null || result.Data == null)
            {
                ErrorMessage = "Failed to load businesses.";
                Businesses.Clear();
                return;
            }

            Businesses.Clear();
            foreach (var business in result.Data.OrderBy(b => b.Name))
            {
                Businesses.Add(business);
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
