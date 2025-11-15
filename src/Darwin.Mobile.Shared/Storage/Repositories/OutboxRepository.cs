using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Mobile.Shared.Common;

namespace Darwin.Mobile.Shared.Storage.Repositories;

/// <summary>
/// In-memory outbox repository; replace with SQLite later.
/// </summary>
public sealed class OutboxRepository
{
    private readonly ApiOptions _opts;
    private readonly ConcurrentQueue<Outbox.OutboxMessage> _q = new();

    public OutboxRepository(ApiOptions opts) { _opts = opts; }

    public Task EnqueueAsync(Outbox.OutboxMessage msg)
    {
        if (_q.Count >= _opts.MaxOutbox) _ = _q.TryDequeue(out _);
        _q.Enqueue(msg);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Outbox.OutboxMessage>> SnapshotAsync()
        => Task.FromResult((IReadOnlyList<Outbox.OutboxMessage>)_q.ToList());
}
