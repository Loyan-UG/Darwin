using Darwin.Mobile.Shared.Storage.Outbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Storage.Abstractions
{
    public interface IOutboxRepository
    {
        Task EnqueueAsync(string path, string method, string jsonBody, CancellationToken ct);
        Task<IReadOnlyList<OutboxMessage>> DequeueBatchAsync(int maxCount, CancellationToken ct);
        Task MarkAsSucceededAsync(string id, CancellationToken ct);
        Task MarkAsFailedAsync(string id, string? error, CancellationToken ct);
    }
}
