using Darwin.Contracts.Businesses;
using Darwin.Mobile.Shared.Api;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Services;

/// <summary>
/// Loads current business operational access state for authenticated business clients.
/// </summary>
public interface IBusinessAccessService
{
    /// <summary>
    /// Returns the current access-state snapshot for the authenticated business operator.
    /// </summary>
    Task<Result<BusinessAccessStateResponse>> GetCurrentAccessStateAsync(CancellationToken ct);
}

/// <summary>
/// Default API-backed implementation of <see cref="IBusinessAccessService"/>.
/// </summary>
public sealed class BusinessAccessService : IBusinessAccessService
{
    private readonly IApiClient _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessAccessService"/> class.
    /// </summary>
    public BusinessAccessService(IApiClient api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <inheritdoc />
    public Task<Result<BusinessAccessStateResponse>> GetCurrentAccessStateAsync(CancellationToken ct)
        => _api.GetResultAsync<BusinessAccessStateResponse>(ApiRoutes.BusinessAccount.GetAccessState, ct);
}
