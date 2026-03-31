using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Storage.Abstractions;
using Darwin.Mobile.Shared.Storage.Outbox;
using Darwin.Mobile.Shared.Storage.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Storage.Repositories;

/// <summary>
/// SQLite-backed outbox repository used to persist queued mutations across app restarts.
/// </summary>
public sealed class OutboxRepository : IOutboxRepository
{
    private readonly ApiOptions _opts;
    private readonly LocalDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxRepository"/> class.
    /// </summary>
    public OutboxRepository(ApiOptions opts, LocalDatabase database)
    {
        _opts = opts ?? throw new ArgumentNullException(nameof(opts));
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    /// <inheritdoc />
    public async Task EnqueueAsync(string path, string method, string jsonBody, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var totalCount = await connection.Table<OutboxMessageRecord>().CountAsync().ConfigureAwait(false);
        if (totalCount >= _opts.MaxOutbox)
        {
            var oldest = await connection.Table<OutboxMessageRecord>()
                .OrderBy(x => x.EnqueuedAtUtc)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (oldest is not null)
            {
                await connection.DeleteAsync(oldest).ConfigureAwait(false);
            }
        }

        await connection.InsertAsync(new OutboxMessageRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Path = path,
            Method = string.IsNullOrWhiteSpace(method) ? "POST" : method.Trim().ToUpperInvariant(),
            JsonBody = string.IsNullOrWhiteSpace(jsonBody) ? "{}" : jsonBody,
            EnqueuedAtUtc = DateTime.UtcNow,
            Attempts = 0,
            IsSucceeded = false
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OutboxMessage>> DequeueBatchAsync(int maxCount, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var boundedCount = Math.Max(1, maxCount);
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var records = await connection.Table<OutboxMessageRecord>()
            .Where(x => !x.IsSucceeded)
            .OrderBy(x => x.EnqueuedAtUtc)
            .Take(boundedCount)
            .ToListAsync()
            .ConfigureAwait(false);

        return records
            .Select(static record => new OutboxMessage
            {
                Id = record.Id,
                Path = record.Path,
                Method = record.Method,
                JsonBody = record.JsonBody,
                EnqueuedAtUtc = record.EnqueuedAtUtc,
                Attempts = record.Attempts
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task MarkAsSucceededAsync(string id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        await connection.DeleteAsync<OutboxMessageRecord>(id).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MarkAsFailedAsync(string id, string? error, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var record = await connection.Table<OutboxMessageRecord>()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (record is null)
        {
            return;
        }

        record.Attempts += 1;
        record.LastError = error;
        record.LastAttemptedAtUtc = DateTime.UtcNow;
        await connection.UpdateAsync(record).ConfigureAwait(false);
    }
}
