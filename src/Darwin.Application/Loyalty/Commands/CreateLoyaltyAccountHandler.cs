using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Command handler that creates (joins) a LoyaltyAccount for the current user
    /// in the context of the specified business when one does not already exist.
    /// </summary>
    /// <remarks>
    /// Behaviour:
    /// - If the current user already has a (non-deleted) LoyaltyAccount for the business,
    ///   the handler returns a success result containing a summary DTO for the existing account.
    /// - If no account exists, the handler creates a new one with sensible defaults
    ///   (PointsBalance = 0, LifetimePoints = 0, Status = Active) and returns its summary DTO.
    /// - This handler does NOT expose internal identifiers other than what the DTO contains.
    /// Security:
    /// - The current user id is resolved from <see cref="ICurrentUserService"/>.
    /// - If the current principal cannot be resolved, the handler fails.
    /// Transactionality:
    /// - Uses a single SaveChangesAsync call to keep creation atomic.
    /// </remarks>
    public sealed class CreateLoyaltyAccountHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUserService;
        private readonly IClock _clock;

        public CreateLoyaltyAccountHandler(
            IAppDbContext db,
            ICurrentUserService currentUserService,
            IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Ensures a loyalty account exists for the current user and specified business.
        /// Returns a <see cref="LoyaltyAccountSummaryDto"/> describing the (existing or newly created) account.
        /// </summary>
        /// <param name="businessId">The business for which an account should exist.</param>
        /// <param name="businessLocationId">Optional preferred business location.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Result wrapping the loyalty account summary DTO.</returns>
        public async Task<Result<LoyaltyAccountSummaryDto>> HandleAsync(
            Guid businessId,
            Guid? businessLocationId,
            CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return Result<LoyaltyAccountSummaryDto>.Fail("BusinessId is required.");
            }

            var userId = _currentUserService.GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Result<LoyaltyAccountSummaryDto>.Fail("User is not authenticated.");
            }

            // Try find an existing (non-deleted) loyalty account for (business, user)
            var existing = await _db.Set<LoyaltyAccount>()
                .AsQueryable()
                .SingleOrDefaultAsync(a =>
                    a.BusinessId == businessId &&
                    a.UserId == userId &&
                    !a.IsDeleted,
                    ct)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                // Map to summary DTO and return
                var existingDto = new LoyaltyAccountSummaryDto
                {
                    Id = existing.Id,
                    BusinessId = existing.BusinessId,
                    BusinessName = null, // Business name resolved by higher layers (or left null)
                    PointsBalance = existing.PointsBalance,
                    LifetimePoints = existing.LifetimePoints,
                    Status = existing.Status,
                    LastAccrualAtUtc = existing.LastAccrualAtUtc
                };

                return Result<LoyaltyAccountSummaryDto>.Ok(existingDto);
            }

            // Optionally validate that the business exists and is not deleted.
            // If present in the model, prefer to check for business presence to avoid
            // creating accounts for non-existent businesses.
            var business = await _db.Set<Domain.Entities.Businesses.Business>()
                .AsQueryable()
                .SingleOrDefaultAsync(b => b.Id == businessId && !b.IsDeleted, ct)
                .ConfigureAwait(false);

            if (business is null)
            {
                return Result<LoyaltyAccountSummaryDto>.Fail("Business not found.");
            }

            // Create new loyalty account
            var now = _clock.UtcNow;
            var newAccount = new LoyaltyAccount
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                UserId = userId,
                PointsBalance = 0,
                LifetimePoints = 0,
                Status = LoyaltyAccountStatus.Active,
                CreatedAtUtc = now,
                LastAccrualAtUtc = null,
                IsDeleted = false
            };

            // Persist
            _db.Set<LoyaltyAccount>().Add(newAccount);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            var dto = new LoyaltyAccountSummaryDto
            {
                Id = newAccount.Id,
                BusinessId = newAccount.BusinessId,
                BusinessName = business?.Name, // may be null-safe
                PointsBalance = newAccount.PointsBalance,
                LifetimePoints = newAccount.LifetimePoints,
                Status = newAccount.Status,
                LastAccrualAtUtc = newAccount.LastAccrualAtUtc
            };

            return Result<LoyaltyAccountSummaryDto>.Ok(dto);
        }
    }
}