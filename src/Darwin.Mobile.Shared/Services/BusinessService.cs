using Darwin.Contracts.Businesses;
using Darwin.Contracts.Common;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Caching;
using Darwin.Shared.Results;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Services;

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
    Task<Result<BusinessOnboardingResponse>> OnboardAsync(BusinessOnboardingRequest request, CancellationToken ct);

    Task<Result<BusinessEngagementSummaryResponse>> GetMyEngagementAsync(Guid businessId, CancellationToken ct);
    Task<Result<ToggleBusinessReactionResponse>> ToggleLikeAsync(Guid businessId, CancellationToken ct);
    Task<Result<ToggleBusinessReactionResponse>> ToggleFavoriteAsync(Guid businessId, CancellationToken ct);
    Task<Result> UpsertMyReviewAsync(Guid businessId, UpsertBusinessReviewRequest request, CancellationToken ct);
}

public sealed class BusinessService : IBusinessService
{
    private static readonly JsonSerializerOptions CacheKeyJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan CategoryKindsCacheTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan CategoryKindsFallbackMaxAge = TimeSpan.FromDays(7);
    private static readonly TimeSpan DiscoveryCacheTtl = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan DiscoveryFallbackMaxAge = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan DetailCacheTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DetailFallbackMaxAge = TimeSpan.FromMinutes(20);

    private readonly IApiClient _api;
    private readonly IMobileCacheService _cache;

    public BusinessService(IApiClient api, IMobileCacheService cache)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<PagedResponse<BusinessSummary>?> ListAsync(BusinessListRequest req, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(req);

        var cacheKey = BuildQueryCacheKey("businesses.list", req);
        var cached = await _cache.GetFreshAsync<PagedResponse<BusinessSummary>>(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var response = await _api.PostAsync<BusinessListRequest, PagedResponse<BusinessSummary>>(ApiRoutes.Businesses.List, req, ct).ConfigureAwait(false);
        if (response is not null)
        {
            await _cache.SetAsync(cacheKey, response, DiscoveryCacheTtl, ct).ConfigureAwait(false);
            return response;
        }

        return await _cache.GetUsableAsync<PagedResponse<BusinessSummary>>(cacheKey, DiscoveryFallbackMaxAge, ct).ConfigureAwait(false);
    }

    public async Task<PagedResponse<BusinessSummary>?> MapAsync(BusinessMapDiscoveryRequest req, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(req);

        var cacheKey = BuildQueryCacheKey("businesses.map", req);
        var cached = await _cache.GetFreshAsync<PagedResponse<BusinessSummary>>(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var response = await _api.PostAsync<BusinessMapDiscoveryRequest, PagedResponse<BusinessSummary>>(ApiRoutes.Businesses.Map, req, ct).ConfigureAwait(false);
        if (response is not null)
        {
            await _cache.SetAsync(cacheKey, response, DiscoveryCacheTtl, ct).ConfigureAwait(false);
            return response;
        }

        return await _cache.GetUsableAsync<PagedResponse<BusinessSummary>>(cacheKey, DiscoveryFallbackMaxAge, ct).ConfigureAwait(false);
    }

    public async Task<BusinessDetail?> GetAsync(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        var cacheKey = $"businesses.detail:{id:D}";
        var cached = await _cache.GetFreshAsync<BusinessDetail>(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var response = await _api.GetAsync<BusinessDetail>(ApiRoutes.Businesses.GetById(id), ct).ConfigureAwait(false);
        if (response is not null)
        {
            await _cache.SetAsync(cacheKey, response, DetailCacheTtl, ct).ConfigureAwait(false);
            return response;
        }

        return await _cache.GetUsableAsync<BusinessDetail>(cacheKey, DetailFallbackMaxAge, ct).ConfigureAwait(false);
    }

    public Task<BusinessDetailWithMyAccount?> GetWithMyAccountAsync(Guid id, CancellationToken ct)
        => _api.GetAsync<BusinessDetailWithMyAccount>(ApiRoutes.Businesses.GetWithMyAccount(id), ct);

    public async Task<BusinessCategoryKindsResponse?> GetCategoryKindsAsync(CancellationToken ct)
    {
        const string cacheKey = "businesses.category-kinds";

        var cached = await _cache.GetFreshAsync<BusinessCategoryKindsResponse>(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var response = await _api.GetAsync<BusinessCategoryKindsResponse>(ApiRoutes.Businesses.CategoryKinds, ct).ConfigureAwait(false);
        if (response is not null)
        {
            await _cache.SetAsync(cacheKey, response, CategoryKindsCacheTtl, ct).ConfigureAwait(false);
            return response;
        }

        return await _cache.GetUsableAsync<BusinessCategoryKindsResponse>(cacheKey, CategoryKindsFallbackMaxAge, ct).ConfigureAwait(false);
    }

    public Task<Result<BusinessOnboardingResponse>> OnboardAsync(BusinessOnboardingRequest request, CancellationToken ct)
        => _api.PostResultAsync<BusinessOnboardingRequest, BusinessOnboardingResponse>(ApiRoutes.Businesses.Onboarding, request, ct);

    public Task<Result<BusinessEngagementSummaryResponse>> GetMyEngagementAsync(Guid businessId, CancellationToken ct)
        => _api.GetResultAsync<BusinessEngagementSummaryResponse>(ApiRoutes.Businesses.GetMyEngagement(businessId), ct);

    public Task<Result<ToggleBusinessReactionResponse>> ToggleLikeAsync(Guid businessId, CancellationToken ct)
        => _api.PutResultAsync<object, ToggleBusinessReactionResponse>(ApiRoutes.Businesses.ToggleLike(businessId), new { }, ct);

    public Task<Result<ToggleBusinessReactionResponse>> ToggleFavoriteAsync(Guid businessId, CancellationToken ct)
        => _api.PutResultAsync<object, ToggleBusinessReactionResponse>(ApiRoutes.Businesses.ToggleFavorite(businessId), new { }, ct);

    public Task<Result> UpsertMyReviewAsync(Guid businessId, UpsertBusinessReviewRequest request, CancellationToken ct)
        => _api.PutNoContentAsync(ApiRoutes.Businesses.UpsertMyReview(businessId), request, ct);

    private static string BuildQueryCacheKey<T>(string prefix, T request)
    {
        var serialized = JsonSerializer.Serialize(request, CacheKeyJsonOptions);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(serialized));
        var hash = Convert.ToHexString(hashBytes);
        return $"{prefix}:{hash}";
    }
}
