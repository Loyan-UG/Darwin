using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Services;

namespace Darwin.Mobile.Shared.Security;

/// <summary>
/// Periodically refreshes a short-lived QR token using the API, raising an event when updated.
/// </summary>
public sealed class QrTokenRefresher : IAsyncDisposable
{
    private readonly ILoyaltyService _loyalty;
    private readonly Func<int> _intervalSecondsProvider;
    private CancellationTokenSource? _cts;

    /// <summary>Raised when a new token has been fetched.</summary>
    public event EventHandler<QrCodePayloadDto>? TokenRefreshed;

    public QrTokenRefresher(ILoyaltyService loyalty, Func<int> intervalSecondsProvider)
    {
        _loyalty = loyalty;
        _intervalSecondsProvider = intervalSecondsProvider;
    }

    /// <summary>Starts background loop. Idempotent.</summary>
    public void Start()
    {
        if (_cts is not null) return;
        _cts = new CancellationTokenSource();
        _ = LoopAsync(_cts.Token);
    }

    /// <summary>Stops background loop.</summary>
    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var token = await _loyalty.GetQrTokenAsync(ct);
                TokenRefreshed?.Invoke(this, token);
            }
            catch { /* TODO: log/telemetry */ }

            var delay = Math.Max(10, _intervalSecondsProvider());
            try { await Task.Delay(TimeSpan.FromSeconds(delay), ct); } catch { }
        }
    }

    public ValueTask DisposeAsync() { Stop(); return ValueTask.CompletedTask; }
}
