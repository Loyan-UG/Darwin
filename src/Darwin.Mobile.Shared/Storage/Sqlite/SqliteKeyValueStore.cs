using Darwin.Mobile.Shared.Storage.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Storage.Sqlite;

/// <summary>
/// SQLite-backed key-value store for lightweight mobile state that should survive app restarts.
/// </summary>
public sealed class SqliteKeyValueStore : IKeyValueStore
{
    private const int MaxKeyLength = 200;
    private const int MaxValueLength = 256 * 1024;

    private readonly LocalDatabase _database;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteKeyValueStore"/> class.
    /// </summary>
    public SqliteKeyValueStore(LocalDatabase database, TimeProvider timeProvider)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task SetAsync(string key, string value, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var normalizedKey = NormalizeKey(key);
        var normalizedValue = NormalizeValue(value);

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        await connection.InsertOrReplaceAsync(new KeyValueRecord
        {
            Key = normalizedKey,
            Value = normalizedValue,
            UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var normalizedKey = NormalizeKey(key);

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var record = await connection.Table<KeyValueRecord>()
            .Where(x => x.Key == normalizedKey)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        return record?.Value;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var normalizedKey = NormalizeKey(key);

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        await connection.DeleteAsync<KeyValueRecord>(normalizedKey).ConfigureAwait(false);
    }

    private static string NormalizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key is required.", nameof(key));
        }

        var normalized = key.Trim();
        if (normalized.Length > MaxKeyLength)
        {
            throw new ArgumentException($"Key cannot exceed {MaxKeyLength} characters.", nameof(key));
        }

        return normalized;
    }

    private static string NormalizeValue(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (value.Length > MaxValueLength)
        {
            throw new ArgumentException($"Value cannot exceed {MaxValueLength} characters.", nameof(value));
        }

        return value;
    }
}
