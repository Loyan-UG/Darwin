using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Storage.Abstractions;
using Darwin.Mobile.Shared.Storage.Outbox;
using Darwin.Mobile.Shared.Storage.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Storage.Repositories;

/// <summary>
/// SQLite-backed outbox repository used to persist queued mutations across app restarts.
/// </summary>
public sealed class OutboxRepository : IOutboxRepository
{
    private static readonly HashSet<string> AllowedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST",
        "PUT",
        "PATCH",
        "DELETE"
    };

    private const int MaxPathLength = 512;
    private const int MaxJsonBodyLength = 32 * 1024;
    private const int MaxLastErrorLength = 512;
    private const int MaxBatchSize = 50;
    private const int MaxAttempts = 8;

    private readonly ApiOptions _opts;
    private readonly LocalDatabase _database;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxRepository"/> class.
    /// </summary>
    public OutboxRepository(ApiOptions opts, LocalDatabase database, TimeProvider timeProvider)
    {
        _opts = opts ?? throw new ArgumentNullException(nameof(opts));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task EnqueueAsync(string path, string method, string jsonBody, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var normalizedPath = NormalizePath(path);
        var normalizedMethod = NormalizeMethod(method);
        var normalizedBody = NormalizeJsonBody(jsonBody);

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var maxOutbox = Math.Max(1, _opts.MaxOutbox);
        var totalCount = await connection.Table<OutboxMessageRecord>().CountAsync().ConfigureAwait(false);
        while (totalCount >= maxOutbox)
        {
            ct.ThrowIfCancellationRequested();

            var oldest = await connection.Table<OutboxMessageRecord>()
                .OrderBy(x => x.EnqueuedAtUtc)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (oldest is not null)
            {
                await connection.DeleteAsync(oldest).ConfigureAwait(false);
                totalCount--;
            }
            else
            {
                break;
            }
        }

        ct.ThrowIfCancellationRequested();
        await connection.InsertAsync(new OutboxMessageRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Path = normalizedPath,
            Method = normalizedMethod,
            JsonBody = normalizedBody,
            EnqueuedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            Attempts = 0,
            IsSucceeded = false
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OutboxMessage>> DequeueBatchAsync(int maxCount, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var boundedCount = Math.Clamp(maxCount, 1, MaxBatchSize);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();

        var records = await connection.Table<OutboxMessageRecord>()
            .Where(x => !x.IsSucceeded && x.Attempts < MaxAttempts)
            .OrderBy(x => x.EnqueuedAtUtc)
            .Take(boundedCount * 4)
            .ToListAsync()
            .ConfigureAwait(false);

        ct.ThrowIfCancellationRequested();
        return records
            .Where(record => IsReadyForRetry(record, nowUtc))
            .Take(boundedCount)
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
        ct.ThrowIfCancellationRequested();
        await connection.DeleteAsync<OutboxMessageRecord>(id).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MarkAsFailedAsync(string id, string? error, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();

        var record = await connection.Table<OutboxMessageRecord>()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (record is null)
        {
            return;
        }

        record.Attempts += 1;
        record.LastError = NormalizeLastError(error);
        record.LastAttemptedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        ct.ThrowIfCancellationRequested();
        await connection.UpdateAsync(record).ConfigureAwait(false);
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Outbox path is required.", nameof(path));
        }

        var normalized = path.Trim().TrimStart('/');
        if (Uri.TryCreate(normalized, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Outbox path must be a relative API path.", nameof(path));
        }

        if (normalized.Length > MaxPathLength)
        {
            throw new ArgumentException($"Outbox path cannot exceed {MaxPathLength} characters.", nameof(path));
        }

        return normalized;
    }

    private static string NormalizeMethod(string method)
    {
        var normalized = string.IsNullOrWhiteSpace(method)
            ? "POST"
            : method.Trim().ToUpperInvariant();

        if (!AllowedMethods.Contains(normalized))
        {
            throw new ArgumentException("Outbox method must be POST, PUT, PATCH, or DELETE.", nameof(method));
        }

        return normalized;
    }

    private static string NormalizeJsonBody(string jsonBody)
    {
        var normalized = string.IsNullOrWhiteSpace(jsonBody) ? "{}" : jsonBody.Trim();
        if (normalized.Length > MaxJsonBodyLength)
        {
            throw new ArgumentException($"Outbox payload cannot exceed {MaxJsonBodyLength} characters.", nameof(jsonBody));
        }

        try
        {
            using var _ = JsonDocument.Parse(normalized);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Outbox payload must be valid JSON.", nameof(jsonBody), ex);
        }

        return normalized;
    }

    private static string? NormalizeLastError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return null;
        }

        var normalized = error.Trim();
        return normalized.Length <= MaxLastErrorLength
            ? normalized
            : normalized[..MaxLastErrorLength].TrimEnd() + "...";
    }

    private static bool IsReadyForRetry(OutboxMessageRecord record, DateTime nowUtc)
    {
        if (!record.LastAttemptedAtUtc.HasValue || record.Attempts <= 0)
        {
            return true;
        }

        var delaySeconds = Math.Min(300, Math.Pow(2, Math.Min(record.Attempts, 8)));
        return record.LastAttemptedAtUtc.Value.AddSeconds(delaySeconds) <= nowUtc;
    }
}
