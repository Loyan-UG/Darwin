using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Commands
{
    public sealed class ToggleBusinessLikeHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public ToggleBusinessLikeHandler(IAppDbContext db, ICurrentUserService currentUser)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        public async Task<Result<ToggleBusinessReactionDto>> HandleAsync(Guid businessId, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
                return Result<ToggleBusinessReactionDto>.Fail("Business id must not be empty.");

            var userId = _currentUser.GetCurrentUserId();

            var existing = await _db.Set<BusinessLike>()
                .SingleOrDefaultAsync(x => x.BusinessId == businessId && x.UserId == userId, ct)
                .ConfigureAwait(false);

            var isActive = existing is null;
            if (existing is null)
            {
                _db.Set<BusinessLike>().Add(new BusinessLike(userId, businessId));
            }
            else
            {
                _db.Set<BusinessLike>().Remove(existing);
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            var count = await BusinessEngagementStatsHelper
                .RecalculateAndGetLikeCountAsync(_db, businessId, ct)
                .ConfigureAwait(false);

            return Result<ToggleBusinessReactionDto>.Ok(new ToggleBusinessReactionDto
            {
                IsActive = isActive,
                TotalCount = count
            });
        }
    }
}