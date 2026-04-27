using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Domain.Entities.CartCheckout;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CartCheckout.Queries
{
    /// <summary>
    /// Returns a computed summary of a cart by either UserId or AnonymousId.
    /// Uses stored snapshots (unit net, VAT, add-on deltas) to produce totals.
    /// </summary>
    public sealed class GetCartSummaryHandler
    {
        private readonly IAppDbContext _db;
        private readonly ComputeCartSummaryHandler _computeCartSummaryHandler;

        public GetCartSummaryHandler(IAppDbContext db, ComputeCartSummaryHandler computeCartSummaryHandler)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _computeCartSummaryHandler = computeCartSummaryHandler ?? throw new ArgumentNullException(nameof(computeCartSummaryHandler));
        }

        public async Task<CartSummaryDto?> HandleAsync(Guid? userId, string? anonId, string? culture = null, CancellationToken ct = default)
        {
            var cart = await _db.Set<Cart>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    !c.IsDeleted &&
                    ((userId != null && c.UserId == userId) ||
                     (userId == null && c.AnonymousId == anonId)),
                    ct);

            return cart is null
                ? null
                : await _computeCartSummaryHandler.HandleAsync(cart.Id, culture, ct).ConfigureAwait(false);
        }
    }
}
