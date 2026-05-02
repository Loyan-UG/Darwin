using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Caching;
using Darwin.Mobile.Shared.Security;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Shared.Results;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
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
    private readonly ITokenStore _tokenStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerLoyaltySnapshotCache"/> class.
    /// </summary>
    public ConsumerLoyaltySnapshotCache(
        ILoyaltyService loyaltyService,
        IMobileCacheService cache,
        ITokenStore tokenStore)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LoyaltyAccountSummary>>> GetMyAccountsAsync(CancellationToken ct)
    {
        var cacheKey = await GetScopedCacheKeyAsync(MyAccountsCacheKey).ConfigureAwait(false);
        var cached = await _cache.GetFreshAsync<IReadOnlyList<LoyaltyAccountSummary>>(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            return Result<IReadOnlyList<LoyaltyAccountSummary>>.Ok(cached);
        }

        var result = await _loyaltyService.GetMyAccountsAsync(ct).ConfigureAwait(false);
        if (result.Succeeded && result.Value is not null)
        {
            await _cache.SetAsync(cacheKey, result.Value, SnapshotCacheTtl, ct).ConfigureAwait(false);
            return result;
        }

        var fallback = await _cache.GetUsableAsync<IReadOnlyList<LoyaltyAccountSummary>>(cacheKey, SnapshotFallbackMaxAge, ct).ConfigureAwait(false);
        return fallback is not null
            ? Result<IReadOnlyList<LoyaltyAccountSummary>>.Ok(fallback)
            : result;
    }

    /// <inheritdoc />
    public async Task<Result<MyLoyaltyOverviewResponse>> GetMyOverviewAsync(CancellationToken ct)
    {
        var cacheKey = await GetScopedCacheKeyAsync(MyOverviewCacheKey).ConfigureAwait(false);
        var cached = await _cache.GetFreshAsync<MyLoyaltyOverviewResponse>(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            return Result<MyLoyaltyOverviewResponse>.Ok(cached);
        }

        var result = await _loyaltyService.GetMyOverviewAsync(ct).ConfigureAwait(false);
        if (result.Succeeded && result.Value is not null)
        {
            await _cache.SetAsync(cacheKey, result.Value, SnapshotCacheTtl, ct).ConfigureAwait(false);
            return result;
        }

        var fallback = await _cache.GetUsableAsync<MyLoyaltyOverviewResponse>(cacheKey, SnapshotFallbackMaxAge, ct).ConfigureAwait(false);
        return fallback is not null
            ? Result<MyLoyaltyOverviewResponse>.Ok(fallback)
            : result;
    }

    /// <inheritdoc />
    public async Task InvalidateAsync(CancellationToken ct)
    {
        await _cache.RemoveAsync(await GetScopedCacheKeyAsync(MyAccountsCacheKey).ConfigureAwait(false), ct).ConfigureAwait(false);
        await _cache.RemoveAsync(await GetScopedCacheKeyAsync(MyOverviewCacheKey).ConfigureAwait(false), ct).ConfigureAwait(false);
    }

    private async Task<string> GetScopedCacheKeyAsync(string baseKey)
    {
        var (accessToken, _) = await _tokenStore.GetAccessAsync().ConfigureAwait(false);
        var subject = JwtClaimReader.GetSubject(accessToken);
        return string.IsNullOrWhiteSpace(subject)
            ? $"{baseKey}:{BuildFallbackScope(accessToken)}"
            : $"{baseKey}:{subject}";
    }

    /// <summary>
    /// Builds a non-readable cache scope when the JWT subject cannot be parsed.
    /// This prevents loyalty snapshots from falling back to a shared unscoped key.
    /// </summary>
    private static string BuildFallbackScope(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return "anonymous";
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(accessToken.Trim()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
