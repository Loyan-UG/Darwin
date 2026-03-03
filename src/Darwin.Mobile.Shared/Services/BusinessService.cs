using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Common;
using Darwin.Mobile.Shared.Api;
using Darwin.Shared.Results;

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

        Task<Result<BusinessEngagementSummaryResponse>> GetMyEngagementAsync(Guid businessId, CancellationToken ct);
        Task<Result<ToggleBusinessReactionResponse>> ToggleLikeAsync(Guid businessId, CancellationToken ct);
        Task<Result<ToggleBusinessReactionResponse>> ToggleFavoriteAsync(Guid businessId, CancellationToken ct);
        Task<Result> UpsertMyReviewAsync(Guid businessId, UpsertBusinessReviewRequest request, CancellationToken ct);
    }

    public sealed class BusinessService : IBusinessService
    {
        private readonly IApiClient _api;

        public BusinessService(IApiClient api) => _api = api ?? throw new ArgumentNullException(nameof(api));

        public Task<PagedResponse<BusinessSummary>?> ListAsync(BusinessListRequest req, CancellationToken ct)
            => _api.PostAsync<BusinessListRequest, PagedResponse<BusinessSummary>>(ApiRoutes.Businesses.List, req, ct);

        public Task<PagedResponse<BusinessSummary>?> MapAsync(BusinessMapDiscoveryRequest req, CancellationToken ct)
            => _api.PostAsync<BusinessMapDiscoveryRequest, PagedResponse<BusinessSummary>>(ApiRoutes.Businesses.Map, req, ct);

        public Task<BusinessDetail?> GetAsync(Guid id, CancellationToken ct)
            => _api.GetAsync<BusinessDetail>(ApiRoutes.Businesses.GetById(id), ct);

        public Task<BusinessDetailWithMyAccount?> GetWithMyAccountAsync(Guid id, CancellationToken ct)
            => _api.GetAsync<BusinessDetailWithMyAccount>(ApiRoutes.Businesses.GetWithMyAccount(id), ct);

        public Task<BusinessCategoryKindsResponse?> GetCategoryKindsAsync(CancellationToken ct)
            => _api.GetAsync<BusinessCategoryKindsResponse>(ApiRoutes.Businesses.CategoryKinds, ct);

        public Task<Result<BusinessEngagementSummaryResponse>> GetMyEngagementAsync(Guid businessId, CancellationToken ct)
            => _api.GetResultAsync<BusinessEngagementSummaryResponse>(ApiRoutes.Businesses.GetMyEngagement(businessId), ct);

        public Task<Result<ToggleBusinessReactionResponse>> ToggleLikeAsync(Guid businessId, CancellationToken ct)
            => _api.PutResultAsync<object, ToggleBusinessReactionResponse>(ApiRoutes.Businesses.ToggleLike(businessId), new { }, ct);

        public Task<Result<ToggleBusinessReactionResponse>> ToggleFavoriteAsync(Guid businessId, CancellationToken ct)
            => _api.PutResultAsync<object, ToggleBusinessReactionResponse>(ApiRoutes.Businesses.ToggleFavorite(businessId), new { }, ct);

        public Task<Result> UpsertMyReviewAsync(Guid businessId, UpsertBusinessReviewRequest request, CancellationToken ct)
            => _api.PutNoContentAsync(ApiRoutes.Businesses.UpsertMyReview(businessId), request, ct);
    }
}