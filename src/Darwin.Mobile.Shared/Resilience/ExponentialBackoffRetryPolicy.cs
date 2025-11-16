using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Resilience
{
    /// <summary>
    /// Simple exponential backoff retry policy with jitter.
    /// Retries on <see cref="HttpRequestException"/> and <see cref="TaskCanceledException"/> (timeout).
    /// NOT a general-purpose policy. Keep the max attempts small to preserve battery and UX.
    /// </summary>
    public sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
    {
        private readonly int _maxAttempts;
        private readonly TimeSpan _baseDelay;
        private readonly Random _rng = new();

        /// <summary>
        /// Creates a new retry policy.
        /// </summary>
        /// <param name="maxAttempts">Maximum number of attempts including the first call. Default is 3.</param>
        /// <param name="baseDelay">Base delay for backoff. Default is 300ms.</param>
        public ExponentialBackoffRetryPolicy(int maxAttempts = 3, TimeSpan? baseDelay = null)
        {
            _maxAttempts = Math.Max(1, maxAttempts);
            _baseDelay = baseDelay ?? TimeSpan.FromMilliseconds(300);
        }

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
                    if (attempt == _maxAttempts) break;

                    var backoff = ComputeDelay(attempt);
                    try { await Task.Delay(backoff, ct).ConfigureAwait(false); }
                    catch (TaskCanceledException) { throw; }
                }
            }

            throw last ?? new InvalidOperationException("Operation failed without exception.");
        }

        private bool IsTransient(Exception ex)
            => ex is HttpRequestException || ex is TaskCanceledException;

        private TimeSpan ComputeDelay(int attempt)
        {
            // exponential backoff with jitter
            var factor = Math.Pow(2, attempt - 1);
            var millis = _baseDelay.TotalMilliseconds * factor;
            var jitter = _rng.Next(50, 150);
            var total = Math.Min(millis + jitter, 5000); // clamp
            return TimeSpan.FromMilliseconds(total);
        }
    }
}
