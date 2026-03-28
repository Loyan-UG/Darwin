using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Queries
{
    /// <summary>
    /// Loads one loyalty account for admin detail and action screens.
    /// </summary>
    public sealed class GetLoyaltyAccountForAdminHandler
    {
        private readonly IAppDbContext _db;

        public GetLoyaltyAccountForAdminHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<LoyaltyAccountAdminListItemDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return await (
                from account in _db.Set<LoyaltyAccount>().AsNoTracking()
                join user in _db.Set<User>().AsNoTracking() on account.UserId equals user.Id
                where account.Id == id && !account.IsDeleted
                select new LoyaltyAccountAdminListItemDto
                {
                    Id = account.Id,
                    BusinessId = account.BusinessId,
                    UserId = account.UserId,
                    UserEmail = user.Email,
                    UserDisplayName =
                        string.IsNullOrWhiteSpace(((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim())
                            ? user.Email
                            : ((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim(),
                    Status = account.Status,
                    PointsBalance = account.PointsBalance,
                    LifetimePoints = account.LifetimePoints,
                    LastAccrualAtUtc = account.LastAccrualAtUtc,
                    RowVersion = account.RowVersion
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }
    }
}
