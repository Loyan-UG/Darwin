using Darwin.Mobile.Shared.Storage.Outbox;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Storage.Abstractions
{
    /// <summary>
    /// Persists queued mutation requests that can be replayed after transient offline or server failures.
    /// </summary>
    public interface IOutboxRepository
    {
        /// <summary>
        /// Adds a normalized API mutation to the local outbox while enforcing the configured queue size limit.
        /// </summary>
        /// <param name="path">Relative API path. Absolute URLs are rejected to prevent unsafe replay targets.</param>
        /// <param name="method">HTTP method for the mutation.</param>
        /// <param name="jsonBody">Serialized JSON body to replay later.</param>
        /// <param name="ct">Cancellation token for the local storage operation.</param>
        Task EnqueueAsync(string path, string method, string jsonBody, CancellationToken ct);

        /// <summary>
        /// Dequeues retry-ready messages in FIFO order with exponential retry backoff applied.
        /// </summary>
        /// <param name="maxCount">Maximum number of messages to return.</param>
        /// <param name="ct">Cancellation token for the local storage operation.</param>
        /// <returns>Retry-ready outbox messages.</returns>
        Task<IReadOnlyList<OutboxMessage>> DequeueBatchAsync(int maxCount, CancellationToken ct);

        /// <summary>
        /// Removes a successfully replayed message from the local queue.
        /// </summary>
        /// <param name="id">Outbox message identifier.</param>
        /// <param name="ct">Cancellation token for the local storage operation.</param>
        Task MarkAsSucceededAsync(string id, CancellationToken ct);

        /// <summary>
        /// Records a failed replay attempt and stores a bounded diagnostic message for later support analysis.
        /// </summary>
        /// <param name="id">Outbox message identifier.</param>
        /// <param name="error">Optional replay error summary.</param>
        /// <param name="ct">Cancellation token for the local storage operation.</param>
        Task MarkAsFailedAsync(string id, string? error, CancellationToken ct);
    }
}
