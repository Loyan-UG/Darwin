using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Loyalty;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Queries
{
    /// <summary>
    /// Query handler that retrieves an existing loyalty account by (BusinessId, UserId).
    /// Returns null when not found. This handler performs no mutations.
    /// </summary>
    public sealed class GetLoyaltyAccountByBusinessAndUserHandler
    {
        private readonly IAppDbContext _db;

        public GetLoyaltyAccountByBusinessAndUserHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Retrieves an existing loyalty account. Returns null when not found.
        /// </summary>
        public async Task<LoyaltyAccountDto?> HandleAsync(Guid businessId, Guid userId, CancellationToken ct = default)
        {
            var entity = await _db.Set<LoyaltyAccount>()
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.BusinessId == businessId && a.UserId == userId, ct);

            if (entity is null)
                return null;

            return new LoyaltyAccountDto
            {
                Id = entity.Id,
                BusinessId = entity.BusinessId,
                UserId = entity.UserId,
                Status = entity.Status,
                PointsBalance = entity.PointsBalance,
                LifetimePoints = entity.LifetimePoints,
                LastAccrualAtUtc = entity.LastAccrualAtUtc,
                RowVersion = entity.RowVersion
            };
        }
    }
}
