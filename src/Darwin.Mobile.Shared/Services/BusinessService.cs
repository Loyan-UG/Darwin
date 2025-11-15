using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Common;
using Darwin.Mobile.Shared.Api;

namespace Darwin.Mobile.Shared.Services;

/// <summary>
/// Discovery endpoints for listing businesses and viewing details.
/// </summary>
public interface IBusinessService
{
    Task<PagedResponse<BusinessSummaryDto>> ListAsync(BusinessListRequest req, CancellationToken ct);
    Task<BusinessDetailDto> GetAsync(string id, CancellationToken ct);
}

public sealed class BusinessService : IBusinessService
{
    private readonly IApiClient _api;
    public BusinessService(IApiClient api) => _api = api;

    public Task<PagedResponse<BusinessSummaryDto>> ListAsync(BusinessListRequest req, CancellationToken ct)
        => _api.PostAsync<BusinessListRequest, PagedResponse<BusinessSummaryDto>>("biz/list", req, ct)!;

    public Task<BusinessDetailDto> GetAsync(string id, CancellationToken ct)
        => _api.GetAsync<BusinessDetailDto>($"biz/{id}", ct)!;
}
