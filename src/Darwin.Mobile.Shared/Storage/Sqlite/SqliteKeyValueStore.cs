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
    private readonly LocalDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteKeyValueStore"/> class.
    /// </summary>
    public SqliteKeyValueStore(LocalDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    /// <inheritdoc />
    public async Task SetAsync(string key, string value, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        await connection.InsertOrReplaceAsync(new KeyValueRecord
        {
            Key = key,
            Value = value,
            UpdatedAtUtc = DateTime.UtcNow
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var record = await connection.Table<KeyValueRecord>()
            .Where(x => x.Key == key)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        return record?.Value;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        await connection.DeleteAsync<KeyValueRecord>(key).ConfigureAwait(false);
    }
}
