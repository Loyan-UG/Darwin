using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Resilience
{
    /// <summary>
    /// Represents an asynchronous retry policy abstraction for transient-failure handling.
    /// This contract is intentionally Polly-friendly: you can adapt it to a Polly policy later
    /// without changing call sites across the shared library.
    /// </summary>
    public interface IRetryPolicy
    {
        /// <summary>
        /// Executes the provided asynchronous delegate under a retry policy.
        /// Implementations should only retry on transient exceptions (e.g., HttpRequestException, timeouts).
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="ct">Cancellation token.</param>
        Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct);
    }
}
