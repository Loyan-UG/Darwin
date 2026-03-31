using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.Services.Startup;

/// <summary>
/// Coordinates non-blocking authenticated startup warmup for the Consumer app.
/// </summary>
public interface IConsumerStartupWarmupCoordinator
{
    /// <summary>
    /// Starts best-effort warmup of high-value authenticated data for the current session.
    /// </summary>
    Task WarmAuthenticatedExperienceAsync(CancellationToken ct);
}
