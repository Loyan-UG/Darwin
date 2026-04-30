using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application;
using Darwin.Domain.Entities.Businesses;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Commands
{
    public sealed class ToggleBusinessLikeHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public ToggleBusinessLikeHandler(
            IAppDbContext db,
            ICurrentUserService currentUser,
            IStringLocalizer<ValidationResource>? localizer = null, IClock? clock = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
        }

        public async Task<Result<ToggleBusinessReactionDto>> HandleAsync(Guid businessId, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
                return Result<ToggleBusinessReactionDto>.Fail(_localizer["BusinessIdRequired"]);

            var userId = _currentUser.GetCurrentUserId();
            if (userId == Guid.Empty)
                return Result<ToggleBusinessReactionDto>.Fail(_localizer["UserNotAuthenticated"]);

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
                .RecalculateAndGetLikeCountAsync(_db, _clock, businessId, ct)
                .ConfigureAwait(false);

            return Result<ToggleBusinessReactionDto>.Ok(new ToggleBusinessReactionDto
            {
                IsActive = isActive,
                TotalCount = count
            });
        }
    }
}

