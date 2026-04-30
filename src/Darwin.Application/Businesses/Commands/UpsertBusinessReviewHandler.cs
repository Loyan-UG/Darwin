using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Commands
{
    public sealed class UpsertBusinessReviewHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpsertBusinessReviewHandler(
            IAppDbContext db,
            ICurrentUserService currentUser,
            IStringLocalizer<ValidationResource>? localizer = null, IClock? clock = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
        }

        public async Task<Result> HandleAsync(Guid businessId, UpsertBusinessReviewDto dto, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
                return Result.Fail(_localizer["BusinessIdRequired"]);

            if (dto is null)
                return Result.Fail(_localizer["RequestPayloadRequired"]);

            if (dto.Rating < 1 || dto.Rating > 5)
                return Result.Fail(_localizer["RatingMustBeBetweenOneAndFive"]);

            var userId = _currentUser.GetCurrentUserId();

            var existing = await _db.Set<BusinessReview>()
                .SingleOrDefaultAsync(x => x.BusinessId == businessId && x.UserId == userId && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (existing is null)
            {
                _db.Set<BusinessReview>().Add(new BusinessReview(userId, businessId, dto.Rating, dto.Comment));
            }
            else
            {
                existing.Update(dto.Rating, dto.Comment);
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            await BusinessEngagementStatsHelper.RecalculateAsync(_db, _clock, businessId, ct).ConfigureAwait(false);

            return Result.Ok();
        }
    }
}

