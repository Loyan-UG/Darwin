using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Caching;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Shared.Results;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.Services.Caching;

/// <summary>
/// Local-cache facade for consumer loyalty summary payloads.
/// </summary>
/// <remarks>
/// Performance rationale:
/// - Discover, Feed, and Rewards all need overlapping loyalty summary data.
/// - Centralizing cache policy here avoids repeating TTL/fallback logic in multiple view models.
/// - The cache window stays intentionally short so loyalty mutations converge quickly even without push invalidation.
/// </remarks>
public sealed class ConsumerLoyaltySnapshotCache : IConsumerLoyaltySnapshotCache
{
    private static readonly TimeSpan SnapshotCacheTtl = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan SnapshotFallbackMaxAge = TimeSpan.FromMinutes(10);
    private const string MyAccountsCacheKey = "consumer.loyalty.my-accounts";
    private const string MyOverviewCacheKey = "consumer.loyalty.my-overview";

    private readonly ILoyaltyService _loyaltyService;
    private readonly IMobileCacheService _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerLoyaltySnapshotCache"/> class.
    /// </summary>
    public ConsumerLoyaltySnapshotCache(ILoyaltyService loyaltyService, IMobileCacheService cache)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LoyaltyAccountSummary>>> GetMyAccountsAsync(CancellationToken ct)
    {
        var cached = await _cache.GetFreshAsync<IReadOnlyList<LoyaltyAccountSummary>>(MyAccountsCacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            return Result<IReadOnlyList<LoyaltyAccountSummary>>.Ok(cached);
        }

        var result = await _loyaltyService.GetMyAccountsAsync(ct).ConfigureAwait(false);
        if (result.Succeeded && result.Value is not null)
        {
            await _cache.SetAsync(MyAccountsCacheKey, result.Value, SnapshotCacheTtl, ct).ConfigureAwait(false);
            return result;
        }

        var fallback = await _cache.GetUsableAsync<IReadOnlyList<LoyaltyAccountSummary>>(MyAccountsCacheKey, SnapshotFallbackMaxAge, ct).ConfigureAwait(false);
        return fallback is not null
            ? Result<IReadOnlyList<LoyaltyAccountSummary>>.Ok(fallback)
            : result;
    }

    /// <inheritdoc />
    public async Task<Result<MyLoyaltyOverviewResponse>> GetMyOverviewAsync(CancellationToken ct)
    {
        var cached = await _cache.GetFreshAsync<MyLoyaltyOverviewResponse>(MyOverviewCacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            return Result<MyLoyaltyOverviewResponse>.Ok(cached);
        }

        var result = await _loyaltyService.GetMyOverviewAsync(ct).ConfigureAwait(false);
        if (result.Succeeded && result.Value is not null)
        {
            await _cache.SetAsync(MyOverviewCacheKey, result.Value, SnapshotCacheTtl, ct).ConfigureAwait(false);
            return result;
        }

        var fallback = await _cache.GetUsableAsync<MyLoyaltyOverviewResponse>(MyOverviewCacheKey, SnapshotFallbackMaxAge, ct).ConfigureAwait(false);
        return fallback is not null
            ? Result<MyLoyaltyOverviewResponse>.Ok(fallback)
            : result;
    }

    /// <inheritdoc />
    public async Task InvalidateAsync(CancellationToken ct)
    {
        await _cache.RemoveAsync(MyAccountsCacheKey, ct).ConfigureAwait(false);
        await _cache.RemoveAsync(MyOverviewCacheKey, ct).ConfigureAwait(false);
    }
}
