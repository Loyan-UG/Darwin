using Darwin.Contracts.Businesses;
using Darwin.Mobile.Consumer.Services.Caching;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Services.Profile;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.Services.Startup;

/// <summary>
/// Best-effort authenticated warmup pipeline for the Consumer app.
/// </summary>
/// <remarks>
/// Scope:
/// - Prepares data that multiple first-run tabs need immediately after login or resume.
/// - Runs fully in the background so navigation remains responsive.
/// - Uses a cooldown guard to avoid repeating expensive reads during rapid shell transitions.
/// </remarks>
public sealed class ConsumerStartupWarmupCoordinator : IConsumerStartupWarmupCoordinator
{
    private static readonly TimeSpan WarmupCooldown = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan WarmupTimeout = TimeSpan.FromSeconds(20);

    private readonly IProfileService _profileService;
    private readonly IBusinessService _businessService;
    private readonly IConsumerLoyaltySnapshotCache _loyaltySnapshotCache;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private DateTime _lastWarmupCompletedUtc;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerStartupWarmupCoordinator"/> class.
    /// </summary>
    public ConsumerStartupWarmupCoordinator(
        IProfileService profileService,
        IBusinessService businessService,
        IConsumerLoyaltySnapshotCache loyaltySnapshotCache,
        TimeProvider timeProvider)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        _loyaltySnapshotCache = loyaltySnapshotCache ?? throw new ArgumentNullException(nameof(loyaltySnapshotCache));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task WarmAuthenticatedExperienceAsync(CancellationToken ct)
    {
        if (!_gate.Wait(0))
        {
            return;
        }

        try
        {
            if (_lastWarmupCompletedUtc > _timeProvider.GetUtcNow().UtcDateTime.Subtract(WarmupCooldown))
            {
                return;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(WarmupTimeout);

            var warmupToken = timeoutCts.Token;
            await Task.WhenAll(
                WarmProfileAsync(warmupToken),
                WarmLoyaltyAsync(warmupToken),
                WarmDiscoveryAsync(warmupToken)).ConfigureAwait(false);

            _lastWarmupCompletedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Timeout is intentionally silent because warmup is an optimization, not a functional requirement.
        }
        catch
        {
            // Warmup must never surface failures into startup UX.
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task WarmProfileAsync(CancellationToken ct)
    {
        _ = await _profileService.GetMeAsync(ct).ConfigureAwait(false);
        _ = await _profileService.GetAddressesAsync(ct).ConfigureAwait(false);
        _ = await _profileService.GetLinkedCustomerContextAsync(ct).ConfigureAwait(false);
    }

    private async Task WarmLoyaltyAsync(CancellationToken ct)
    {
        _ = await _loyaltySnapshotCache.GetMyAccountsAsync(ct).ConfigureAwait(false);
        _ = await _loyaltySnapshotCache.GetMyOverviewAsync(ct).ConfigureAwait(false);
    }

    private async Task WarmDiscoveryAsync(CancellationToken ct)
    {
        _ = await _businessService.GetCategoryKindsAsync(ct).ConfigureAwait(false);
        _ = await _businessService.ListAsync(new BusinessListRequest
        {
            Page = 1,
            PageSize = 80
        }, ct).ConfigureAwait(false);
    }
}
