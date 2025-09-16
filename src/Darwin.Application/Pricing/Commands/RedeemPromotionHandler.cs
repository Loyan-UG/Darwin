using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Pricing;

namespace Darwin.Application.Pricing.Commands
{
    /// <summary>
    /// Records a promotion redemption (to be called after order placement) for enforcing caps and reporting.
    /// </summary>
    public sealed class RedeemPromotionHandler
    {
        private readonly IAppDbContext _db;
        public RedeemPromotionHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(Guid promotionId, Guid orderId, Guid? userId, CancellationToken ct = default)
        {
            _db.Set<PromotionRedemption>().Add(new PromotionRedemption
            {
                PromotionId = promotionId,
                OrderId = orderId,
                UserId = userId
            });
            await _db.SaveChangesAsync(ct);
        }
    }
}
