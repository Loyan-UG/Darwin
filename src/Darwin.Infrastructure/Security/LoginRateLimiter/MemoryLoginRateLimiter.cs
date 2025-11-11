using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Security;

namespace Darwin.Infrastructure.Security.LoginRateLimiter
{
    /// <summary>
    /// Sliding-window attempt counter using in-memory storage.
    /// Good for single-node setups. Replace with a Redis-based impl for multi-node.
    /// </summary>
    public sealed class MemoryLoginRateLimiter : ILoginRateLimiter
    {
        private readonly ConcurrentDictionary<string, (int count, DateTime windowStartUtc)> _entries = new();

        public Task<bool> IsAllowedAsync(string key, int maxAttempts, int windowSeconds, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var window = TimeSpan.FromSeconds(Math.Max(1, windowSeconds));
            var entry = _entries.GetOrAdd(key, _ => (0, now));

            if (now - entry.windowStartUtc > window)
            {
                _entries[key] = (0, now);
                return Task.FromResult(true);
            }

            return Task.FromResult(entry.count < maxAttempts);
        }

        public Task RecordAsync(string key, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            _entries.AddOrUpdate(key,
                _ => (1, now),
                (_, old) =>
                {
                    var (count, start) = old;
                    if (now - start > TimeSpan.FromMinutes(1))
                        return (1, now);
                    return (count + 1, start);
                });

            return Task.CompletedTask;
        }
    }
}
