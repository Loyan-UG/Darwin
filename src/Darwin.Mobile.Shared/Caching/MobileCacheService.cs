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
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IKeyValueStore _keyValueStore;
    private readonly SemaphoreSlim _registryGate = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="MobileCacheService"/> class.
    /// </summary>
    public MobileCacheService(IKeyValueStore keyValueStore)
    {
        _keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
    }

    /// <inheritdoc />
    public async Task<T?> GetFreshAsync<T>(string cacheKey, CancellationToken ct)
    {
        var entry = await GetEnvelopeAsync<T>(cacheKey, ct).ConfigureAwait(false);
        if (entry is null || entry.ExpiresAtUtc <= DateTime.UtcNow)
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

        return entry.StoredAtUtc >= DateTime.UtcNow.Subtract(maxAge)
            ? entry.Value
            : default;
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string cacheKey, T value, TimeSpan ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            throw new ArgumentException("Cache key is required.", nameof(cacheKey));
        }

        if (ttl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl), "TTL must be positive.");
        }

        var nowUtc = DateTime.UtcNow;
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

        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            throw new ArgumentException("Cache key is required.", nameof(cacheKey));
        }

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
            if (!keys.Add(storageKey))
            {
                return;
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
            if (!keys.Remove(storageKey))
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

    private async Task<HashSet<string>> LoadRegistryCoreAsync(CancellationToken ct)
    {
        var payload = await _keyValueStore.GetAsync(RegistryStorageKey, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<string>>(payload, JsonOptions) ?? new List<string>();
            return new HashSet<string>(items, StringComparer.Ordinal);
        }
        catch
        {
            await _keyValueStore.RemoveAsync(RegistryStorageKey, ct).ConfigureAwait(false);
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }

    private Task SaveRegistryCoreAsync(HashSet<string> keys, CancellationToken ct)
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

    private sealed class CacheEnvelope<T>
    {
        public DateTime StoredAtUtc { get; init; }
        public DateTime ExpiresAtUtc { get; init; }
        public T? Value { get; init; }
    }
}
