using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Shared.ViewModels;
using Darwin.Mobile.Shared.Services;
using Darwin.Contracts.Common;
using Darwin.Contracts.Businesses;
using Darwin.Mobile.Shared.Commands;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Loads a simple list of businesses for the discovery tab.
/// </summary>
public sealed class DiscoverViewModel : BaseViewModel
{
    private readonly IBusinessService _businessService;
    private bool _loaded;

    public DiscoverViewModel(IBusinessService businessService)
    {
        _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        Businesses = new ObservableCollection<BusinessSummary>();
        RefreshCommand = new AsyncCommand(RefreshAsync);
    }

    public ObservableCollection<BusinessSummary> Businesses { get; }

    public AsyncCommand RefreshCommand { get; }

    public override async Task OnAppearingAsync()
    {
        if (!_loaded)
        {
            await RefreshAsync().ConfigureAwait(false);
            _loaded = true;
        }
    }

    private async Task RefreshAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var request = new BusinessListRequest { Page = 1, PageSize = 25 };
            var response = await _businessService.ListAsync(request, CancellationToken.None)
                                .ConfigureAwait(false);

            Businesses.Clear();

            if (response?.Items != null)
            {
                foreach (var b in response.Items.OrderBy(x => x.Name))
                {
                    Businesses.Add(b);
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
