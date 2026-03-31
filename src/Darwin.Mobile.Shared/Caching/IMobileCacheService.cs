using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Caching;

/// <summary>
/// Provides a lightweight JSON cache over the local key-value store.
/// </summary>
public interface IMobileCacheService
{
    /// <summary>
    /// Gets a cached value only when the entry is still within its freshness window.
    /// </summary>
    Task<T?> GetFreshAsync<T>(string cacheKey, CancellationToken ct);

    /// <summary>
    /// Gets a cached value when the entry age is still acceptable for degraded fallback scenarios.
    /// </summary>
    Task<T?> GetUsableAsync<T>(string cacheKey, TimeSpan maxAge, CancellationToken ct);

    /// <summary>
    /// Stores a value with the specified freshness lifetime.
    /// </summary>
    Task SetAsync<T>(string cacheKey, T value, TimeSpan ttl, CancellationToken ct);

    /// <summary>
    /// Removes a cached value.
    /// </summary>
    Task RemoveAsync(string cacheKey, CancellationToken ct);

    /// <summary>
    /// Removes every cache entry known to the local cache registry.
    /// </summary>
    Task ClearAsync(CancellationToken ct);
}
