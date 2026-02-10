using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Resilience
{
    /// <summary>
    /// Simple exponential backoff retry policy with jitter.
    ///
    /// Rationale:
    /// - Mobile scenarios should retry transient network errors (e.g., temporary connectivity blips, DNS hiccups).
    /// - Keep attempt count small to preserve battery and user experience.
    ///
    /// Pitfalls:
    /// - Do NOT retry non-transient errors (authorization, validation).
    /// - Be careful not to retry on user cancellation (OperationCanceledException).
    /// - Use a thread-safe RNG for jitter when this policy is registered as a singleton.
    ///
    /// Example usage:
    /// services.AddSingleton<IRetryPolicy>(_ => new ExponentialBackoffRetryPolicy(maxAttempts:3));
    /// await policy.ExecuteAsync(async ct =&gt; await httpClient.GetAsync(...), cancellationToken);
    /// </summary>
    public sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
    {
        private readonly int _maxAttempts;
        private readonly TimeSpan _baseDelay;

        /// <summary>
        /// Creates a new policy.
        /// </summary>
        /// <param name="maxAttempts">Maximum attempts including the initial one (must be >=1). Default: 3.</param>
        /// <param name="baseDelay">Base backoff delay. Default: 300ms.</param>
        public ExponentialBackoffRetryPolicy(int maxAttempts = 3, TimeSpan? baseDelay = null)
        {
            _maxAttempts = Math.Max(1, maxAttempts);
            _baseDelay = baseDelay ?? TimeSpan.FromMilliseconds(300);
        }

        /// <inheritdoc />
        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct)
        {
            Exception? last = null;

            for (int attempt = 1; attempt <= _maxAttempts; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    return await operation(ct).ConfigureAwait(false);
                }
                catch (Exception ex) when (IsTransient(ex))
                {
                    last = ex;
                    if (attempt == _maxAttempts)
                        break;

                    var backoff = ComputeDelay(attempt);

                    // Respect caller cancellation while waiting
                    try
                    {
                        await Task.Delay(backoff, ct).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        // If the delay was cancelled by caller, propagate cancellation
                        throw;
                    }
                }
            }

            // If we exhausted retries, rethrow/propagate the last transient exception (avoid hiding it).
            throw last ?? new InvalidOperationException("Operation failed without exception.");
        }

        /// <summary>
        /// Determines whether the exception should be considered transient and thus retried.
        /// We consider low-level transport/timeouts (HttpRequestException, TaskCanceledException due to timeouts).
        /// We intentionally do NOT retry OperationCanceledException (explicit cancellation).
        /// </summary>
        private static bool IsTransient(Exception ex)
            => ex is HttpRequestException || ex is TaskCanceledException;

        /// <summary>
        /// Compute exponential backoff with jitter (milliseconds).
        /// Uses Random.Shared for thread-safe randomness on .NET 6+ / .NET 10.
        /// Clamps to a reasonable upper bound to avoid very long sleeps on mobile.
        /// </summary>
        private TimeSpan ComputeDelay(int attempt)
        {
            // exponential factor: 2^(attempt-1)
            var factor = Math.Pow(2, attempt - 1);
            var millis = _baseDelay.TotalMilliseconds * factor;

            // jitter in a small window (milliseconds)
            var jitter = Random.Shared.Next(50, 150);

            // clamp to 5s to avoid excessively long delays on mobile
            var total = Math.Min(millis + jitter, 5000);
            return TimeSpan.FromMilliseconds(total);
        }
    }
}