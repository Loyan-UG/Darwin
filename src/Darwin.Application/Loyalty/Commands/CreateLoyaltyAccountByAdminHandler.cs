using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Admin-side loyalty account provisioning for users who have not self-enrolled yet.
    /// </summary>
    public sealed class CreateLoyaltyAccountByAdminHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;

        public CreateLoyaltyAccountByAdminHandler(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<Result<LoyaltyAccountAdminListItemDto>> HandleAsync(
            CreateLoyaltyAccountByAdminDto dto,
            CancellationToken ct = default)
        {
            if (dto.BusinessId == Guid.Empty)
            {
                return Result<LoyaltyAccountAdminListItemDto>.Fail("Business is required.");
            }

            if (dto.UserId == Guid.Empty)
            {
                return Result<LoyaltyAccountAdminListItemDto>.Fail("User is required.");
            }

            var business = await _db.Set<Business>()
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == dto.BusinessId && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (business is null)
            {
                return Result<LoyaltyAccountAdminListItemDto>.Fail("Business not found.");
            }

            var user = await _db.Set<User>()
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == dto.UserId && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (user is null)
            {
                return Result<LoyaltyAccountAdminListItemDto>.Fail("User not found.");
            }

            var existing = await _db.Set<LoyaltyAccount>()
                .SingleOrDefaultAsync(x =>
                    x.BusinessId == dto.BusinessId &&
                    x.UserId == dto.UserId &&
                    !x.IsDeleted,
                    ct)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                return Result<LoyaltyAccountAdminListItemDto>.Fail("A loyalty account already exists for this member in the selected business.");
            }

            var account = new LoyaltyAccount
            {
                Id = Guid.NewGuid(),
                BusinessId = dto.BusinessId,
                UserId = dto.UserId,
                Status = LoyaltyAccountStatus.Active,
                PointsBalance = 0,
                LifetimePoints = 0,
                CreatedAtUtc = _clock.UtcNow,
                IsDeleted = false
            };

            _db.Set<LoyaltyAccount>().Add(account);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            var displayName = string.IsNullOrWhiteSpace(((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim())
                ? user.Email
                : ((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim();

            return Result<LoyaltyAccountAdminListItemDto>.Ok(new LoyaltyAccountAdminListItemDto
            {
                Id = account.Id,
                BusinessId = account.BusinessId,
                UserId = account.UserId,
                UserEmail = user.Email,
                UserDisplayName = displayName,
                Status = account.Status,
                PointsBalance = account.PointsBalance,
                LifetimePoints = account.LifetimePoints,
                LastAccrualAtUtc = account.LastAccrualAtUtc,
                RowVersion = account.RowVersion
            });
        }
    }
}
