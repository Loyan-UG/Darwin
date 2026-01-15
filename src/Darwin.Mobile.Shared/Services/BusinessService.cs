using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Common;
using Darwin.Mobile.Shared.Api;

namespace Darwin.Mobile.Shared.Services
{
    /// <summary>
    /// Discovery endpoints for listing businesses, viewing details, and fetching static meta.
    /// Contract-first: consumes Darwin.WebApi endpoints using Darwin.Contracts models.
    /// </summary>
    public interface IBusinessService
    {
        Task<PagedResponse<BusinessSummary>?> ListAsync(BusinessListRequest req, CancellationToken ct);
        Task<PagedResponse<BusinessSummary>?> MapAsync(BusinessMapDiscoveryRequest req, CancellationToken ct);
        Task<BusinessDetail?> GetAsync(Guid id, CancellationToken ct);
        Task<BusinessDetailWithMyAccount?> GetWithMyAccountAsync(Guid id, CancellationToken ct);
        Task<BusinessCategoryKindsResponse?> GetCategoryKindsAsync(CancellationToken ct);
    }

    public sealed class BusinessService : IBusinessService
    {
        private readonly IApiClient _api;

        public BusinessService(IApiClient api) => _api = api ?? throw new ArgumentNullException(nameof(api));

        /// <summary>
        /// Lists businesses for directory-style discovery using POST /api/v1/businesses/list.
        /// </summary>
        public Task<PagedResponse<BusinessSummary>?> ListAsync(BusinessListRequest req, CancellationToken ct)
            => _api.PostAsync<BusinessListRequest, PagedResponse<BusinessSummary>>(ApiRoutes.Businesses.List, req, ct);

        /// <summary>
        /// Performs map viewport discovery using POST /api/v1/businesses/map.
        /// </summary>
        public Task<PagedResponse<BusinessSummary>?> MapAsync(BusinessMapDiscoveryRequest req, CancellationToken ct)
            => _api.PostAsync<BusinessMapDiscoveryRequest, PagedResponse<BusinessSummary>>(ApiRoutes.Businesses.Map, req, ct);

        /// <summary>
        /// Retrieves public business detail using GET /api/v1/businesses/{id}.
        /// </summary>
        public Task<BusinessDetail?> GetAsync(Guid id, CancellationToken ct)
            => _api.GetAsync<BusinessDetail>(ApiRoutes.Businesses.GetById(id), ct);

        /// <summary>
        /// Retrieves public business detail combined with my loyalty account summary using GET /api/v1/businesses/{id}/with-my-account.
        /// Requires member permissions.
        /// </summary>
        public Task<BusinessDetailWithMyAccount?> GetWithMyAccountAsync(Guid id, CancellationToken ct)
            => _api.GetAsync<BusinessDetailWithMyAccount>(ApiRoutes.Businesses.GetWithMyAccount(id), ct);

        /// <summary>
        /// Returns static business category kinds for filters using GET /api/v1/businesses/category-kinds.
        /// </summary>
        public Task<BusinessCategoryKindsResponse?> GetCategoryKindsAsync(CancellationToken ct)
            => _api.GetAsync<BusinessCategoryKindsResponse>(ApiRoutes.Businesses.CategoryKinds, ct);
    }
}