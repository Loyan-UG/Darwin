#nullable enable

using Darwin.Contracts.Common;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Response contract for the current user's "My places" list.
    /// </summary>
    public sealed class MyLoyaltyBusinessesResponse : PagedResponse<MyLoyaltyBusinessSummary>
    {
    }
}
