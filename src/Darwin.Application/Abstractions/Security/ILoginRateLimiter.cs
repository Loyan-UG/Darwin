using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Abstractions.Security
{
    /// <summary>
    /// Minimal login attempt rate limiter. Default Infrastructure impl uses memory cache.
    /// Can be swapped with Redis in distributed setups without changing Application code.
    /// </summary>
    public interface ILoginRateLimiter
    {
        Task<bool> IsAllowedAsync(string key, int maxAttempts, int windowSeconds, CancellationToken ct = default);
        Task RecordAsync(string key, CancellationToken ct = default);
    }
}
