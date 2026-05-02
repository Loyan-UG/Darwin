using Darwin.Mobile.Shared.Storage.Abstractions;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Caching;

/// <summary>
/// JSON cache implementation backed by the shared local key-value store.
/// </summary>
/// <remarks>
/// Design goals:
/// - Avoid repeated network calls for read-heavy mobile screens.
/// - Keep storage keys short and deterministic by hashing logical cache keys.
/// - Allow callers to differentiate between fresh cache hits and degraded stale fallback reads.
/// - Keep a compact registry of active cache entries so logout can clear the entire session cache safely.
/// </remarks>
public sealed class MobileCacheService : IMobileCacheService
{
    private const string RegistryStorageKey = "cache:registry";
    private const int MaxRegistryEntries = 256;
    private const int MaxCacheKeyLength = 512;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IKeyValueStore _keyValueStore;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _registryGate = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="MobileCacheService"/> class.
    /// </summary>
    public MobileCacheService(IKeyValueStore keyValueStore, TimeProvider timeProvider)
    {
        _keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task<T?> GetFreshAsync<T>(string cacheKey, CancellationToken ct)
    {
        var entry = await GetEnvelopeAsync<T>(cacheKey, ct).ConfigureAwait(false);
        if (entry is null || entry.ExpiresAtUtc <= _timeProvider.GetUtcNow().UtcDateTime)
        {
            return default;
        }

        return entry.Value;
    }

    /// <inheritdoc />
    public async Task<T?> GetUsableAsync<T>(string cacheKey, TimeSpan maxAge, CancellationToken ct)
    {
        var entry = await GetEnvelopeAsync<T>(cacheKey, ct).ConfigureAwait(false);
        if (entry is null || maxAge <= TimeSpan.Zero)
        {
            return default;
        }

        return entry.StoredAtUtc >= _timeProvider.GetUtcNow().UtcDateTime.Subtract(maxAge)
            ? entry.Value
            : default;
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string cacheKey, T value, TimeSpan ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        ValidateCacheKey(cacheKey);

        if (ttl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl), "TTL must be positive.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var envelope = new CacheEnvelope<T>
        {
            StoredAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc.Add(ttl),
            Value = value
        };

        var storageKey = ToStorageKey(cacheKey);
        var payload = JsonSerializer.Serialize(envelope, JsonOptions);
        await _keyValueStore.SetAsync(storageKey, payload, ct).ConfigureAwait(false);
        await TrackStorageKeyAsync(storageKey, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string cacheKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        ValidateCacheKey(cacheKey);

        var storageKey = ToStorageKey(cacheKey);
        await _keyValueStore.RemoveAsync(storageKey, ct).ConfigureAwait(false);
        await UntrackStorageKeyAsync(storageKey, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await _registryGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var trackedKeys = await LoadRegistryCoreAsync(ct).ConfigureAwait(false);
            foreach (var storageKey in trackedKeys)
            {
                await _keyValueStore.RemoveAsync(storageKey, ct).ConfigureAwait(false);
            }

            await _keyValueStore.RemoveAsync(RegistryStorageKey, ct).ConfigureAwait(false);
        }
        finally
        {
            _registryGate.Release();
        }
    }

    private async Task<CacheEnvelope<T>?> GetEnvelopeAsync<T>(string cacheKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            return null;
        }

        var storageKey = ToStorageKey(cacheKey);
        var payload = await _keyValueStore.GetAsync(storageKey, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CacheEnvelope<T>>(payload, JsonOptions);
        }
        catch
        {
            await _keyValueStore.RemoveAsync(storageKey, ct).ConfigureAwait(false);
            await UntrackStorageKeyAsync(storageKey, ct).ConfigureAwait(false);
            return null;
        }
    }

    private async Task TrackStorageKeyAsync(string storageKey, CancellationToken ct)
    {
        await _registryGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var keys = await LoadRegistryCoreAsync(ct).ConfigureAwait(false);
            if (keys.Contains(storageKey, StringComparer.Ordinal))
            {
                return;
            }

            keys.Add(storageKey);
            while (keys.Count > MaxRegistryEntries)
            {
                var evictedKey = keys[0];
                keys.RemoveAt(0);
                if (string.IsNullOrWhiteSpace(evictedKey))
                {
                    continue;
                }

                await _keyValueStore.RemoveAsync(evictedKey, ct).ConfigureAwait(false);
            }

            await SaveRegistryCoreAsync(keys, ct).ConfigureAwait(false);
        }
        finally
        {
            _registryGate.Release();
        }
    }

    private async Task UntrackStorageKeyAsync(string storageKey, CancellationToken ct)
    {
        await _registryGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var keys = await LoadRegistryCoreAsync(ct).ConfigureAwait(false);
            var removed = keys.RemoveAll(key => string.Equals(key, storageKey, StringComparison.Ordinal));
            if (removed == 0)
            {
                return;
            }

            if (keys.Count == 0)
            {
                await _keyValueStore.RemoveAsync(RegistryStorageKey, ct).ConfigureAwait(false);
                return;
            }

            await SaveRegistryCoreAsync(keys, ct).ConfigureAwait(false);
        }
        finally
        {
            _registryGate.Release();
        }
    }

    private async Task<List<string>> LoadRegistryCoreAsync(CancellationToken ct)
    {
        var payload = await _keyValueStore.GetAsync(RegistryStorageKey, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return new List<string>();
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<string>>(payload, JsonOptions) ?? new List<string>();
            return DeduplicateRegistry(items);
        }
        catch
        {
            await _keyValueStore.RemoveAsync(RegistryStorageKey, ct).ConfigureAwait(false);
            return new List<string>();
        }
    }

    private Task SaveRegistryCoreAsync(List<string> keys, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(keys, JsonOptions);
        return _keyValueStore.SetAsync(RegistryStorageKey, payload, ct);
    }

    private static string ToStorageKey(string cacheKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(cacheKey.Trim()));
        var builder = new StringBuilder("cache:", 6 + (bytes.Length * 2));
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString();
    }

    private static void ValidateCacheKey(string cacheKey)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            throw new ArgumentException("Cache key is required.", nameof(cacheKey));
        }

        if (cacheKey.Trim().Length > MaxCacheKeyLength)
        {
            throw new ArgumentException($"Cache key cannot exceed {MaxCacheKeyLength} characters.", nameof(cacheKey));
        }
    }

    private sealed class CacheEnvelope<T>
    {
        public DateTime StoredAtUtc { get; init; }
        public DateTime ExpiresAtUtc { get; init; }
        public T? Value { get; init; }
    }

    /// <summary>
    /// Preserves registry insertion order while removing duplicate or empty keys from older payloads.
    /// </summary>
    /// <param name="items">Registry payload loaded from local storage.</param>
    /// <returns>Ordered, distinct cache storage keys.</returns>
    private static List<string> DeduplicateRegistry(List<string> items)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<string>(Math.Min(items.Count, MaxRegistryEntries));

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item) || !seen.Add(item))
            {
                continue;
            }

            ordered.Add(item);
        }

        return ordered;
    }
}
