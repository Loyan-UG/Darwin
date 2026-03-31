using Darwin.Contracts.Loyalty;
using Darwin.Shared.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.Services.Caching;

/// <summary>
/// Provides cached access to consumer loyalty summary payloads that are reused across multiple tabs.
/// </summary>
public interface IConsumerLoyaltySnapshotCache
{
    /// <summary>
    /// Gets joined loyalty accounts with short-lived local caching.
    /// </summary>
    Task<Result<IReadOnlyList<LoyaltyAccountSummary>>> GetMyAccountsAsync(CancellationToken ct);

    /// <summary>
    /// Gets the loyalty overview with short-lived local caching.
    /// </summary>
    Task<Result<MyLoyaltyOverviewResponse>> GetMyOverviewAsync(CancellationToken ct);

    /// <summary>
    /// Removes cached loyalty summary payloads after mutations that can change them.
    /// </summary>
    Task InvalidateAsync(CancellationToken ct);
}
