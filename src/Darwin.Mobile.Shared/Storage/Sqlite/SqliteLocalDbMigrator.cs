using Darwin.Mobile.Shared.Storage.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Storage.Sqlite;

/// <summary>
/// Creates the SQLite schema required by the current mobile shared layer.
/// </summary>
public sealed class SqliteLocalDbMigrator : ILocalDbMigrator
{
    private readonly LocalDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteLocalDbMigrator"/> class.
    /// </summary>
    public SqliteLocalDbMigrator(LocalDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    /// <inheritdoc />
    public async Task EnsureCreatedAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        await connection.CreateTableAsync<KeyValueRecord>().ConfigureAwait(false);
        await connection.CreateTableAsync<OutboxMessageRecord>().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task MigrateAsync(CancellationToken ct) => EnsureCreatedAsync(ct);
}
