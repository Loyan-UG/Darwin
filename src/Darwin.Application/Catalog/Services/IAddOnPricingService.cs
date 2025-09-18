using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Catalog.Services
{
    /// <summary>
    /// Abstraction for validating add-on selections and computing the total price delta
    /// contributed by those selections for a given variant.
    /// Implementations may use database lookups or in-memory catalogs as needed.
    /// </summary>
    public interface IAddOnPricingService
    {
        /// <summary>
        /// Validates that the selected add-on value IDs are applicable to the given variant's product
        /// (respecting group precedence) and satisfy group constraints (required/min/max).
        /// </summary>
        Task ValidateSelectionsForVariantAsync(Guid variantId, IReadOnlyCollection<Guid> selectedValueIds, CancellationToken ct);

        /// <summary>
        /// Computes the sum of price deltas (minor units, net) for the selected add-on values.
        /// Non-active/deleted values must not contribute.
        /// </summary>
        Task<long> SumPriceDeltasAsync(IReadOnlyCollection<Guid> selectedValueIds, CancellationToken ct);
    }
}
