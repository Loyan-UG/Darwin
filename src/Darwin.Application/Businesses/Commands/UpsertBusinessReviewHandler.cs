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
    public sealed class UpsertBusinessReviewHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public UpsertBusinessReviewHandler(IAppDbContext db, ICurrentUserService currentUser)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        public async Task<Result> HandleAsync(Guid businessId, UpsertBusinessReviewDto dto, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
                return Result.Fail("Business id must not be empty.");

            if (dto is null)
                return Result.Fail("Request body is required.");

            if (dto.Rating < 1 || dto.Rating > 5)
                return Result.Fail("Rating must be between 1 and 5.");

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
            await BusinessEngagementStatsHelper.RecalculateAsync(_db, businessId, ct).ConfigureAwait(false);

            return Result.Ok();
        }
    }
}