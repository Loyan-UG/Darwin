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
    /// Loads a loyalty program for edit screens.
    /// </summary>
    public sealed class GetLoyaltyProgramForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetLoyaltyProgramForEditHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<LoyaltyProgramEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Set<LoyaltyProgram>()
                .AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(x => new LoyaltyProgramEditDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    AccrualMode = x.AccrualMode,
                    PointsPerCurrencyUnit = x.PointsPerCurrencyUnit,
                    IsActive = x.IsActive,
                    RulesJson = x.RulesJson,
                    RowVersion = x.RowVersion
                })
                .FirstOrDefaultAsync(ct);
        }
    }
}
