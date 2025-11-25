using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Validators;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Command handler used by business staff to manually adjust a customer's
    /// loyalty points balance. The handler records an <see cref="LoyaltyPointsTransaction"/>
    /// of type <see cref="LoyaltyPointsTransactionType.Adjustment"/> and updates the
    /// corresponding <see cref="LoyaltyAccount"/> balances.
    /// </summary>
    public sealed class AdjustLoyaltyPointsHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly AdjustLoyaltyPointsValidator _validator = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="AdjustLoyaltyPointsHandler"/> class.
        /// </summary>
        /// <param name="db">Application database abstraction used to access loyalty entities.</param>
        /// <param name="clock">Clock abstraction used to obtain current UTC time.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="db"/> or <paramref name="clock"/> is null.
        /// </exception>
        public AdjustLoyaltyPointsHandler(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Applies a manual adjustment to the specified loyalty account.
        /// The method ensures that:
        /// <list type="bullet">
        /// <item><description>The account belongs to the specified business.</description></item>
        /// <item><description>The resulting balance does not become negative.</description></item>
        /// <item><description>Optional optimistic concurrency is enforced when a row version is provided.</description></item>
        /// </list>
        /// </summary>
        /// <param name="dto">Adjustment request containing account id, delta, and audit fields.</param>
        /// <param name="ct">Cancellation token used to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Result{T}"/> containing <see cref="AdjustLoyaltyPointsResultDto"/> on success,
        /// or an error message when validation or business rules fail.
        /// </returns>
        public async Task<Result<AdjustLoyaltyPointsResultDto>> HandleAsync(
            AdjustLoyaltyPointsDto dto,
            CancellationToken ct = default)
        {
            var validationResult = _validator.Validate(dto);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var account = await _db.Set<LoyaltyAccount>()
                .FirstOrDefaultAsync(a => a.Id == dto.LoyaltyAccountId, ct)
                .ConfigureAwait(false);

            if (account is null)
            {
                return Result<AdjustLoyaltyPointsResultDto>.Fail("Loyalty account not found.");
            }

            if (account.BusinessId != dto.BusinessId)
            {
                return Result<AdjustLoyaltyPointsResultDto>.Fail("Business mismatch for loyalty account.");
            }

            // Optional optimistic concurrency check when a row version is supplied.
            if (dto.RowVersion is not null &&
                !account.RowVersion.SequenceEqual(dto.RowVersion))
            {
                return Result<AdjustLoyaltyPointsResultDto>.Fail(
                    "Concurrency conflict. The loyalty account was modified by another process.");
            }

            if (account.Status != LoyaltyAccountStatus.Active)
            {
                return Result<AdjustLoyaltyPointsResultDto>.Fail(
                    "The loyalty account is not active and cannot be adjusted.");
            }

            var newBalance = account.PointsBalance + dto.PointsDelta;
            if (newBalance < 0)
            {
                return Result<AdjustLoyaltyPointsResultDto>.Fail(
                    "The adjustment would result in a negative points balance.");
            }

            var now = _clock.UtcNow;

            // Update account balances. For positive adjustments, the lifetime counter
            // is increased as well. For negative adjustments, the behavior is to also
            // decrease the lifetime aggregate so that analytics reflect the corrected
            // net value rather than the original erroneous accrual.
            account.PointsBalance = newBalance;

            if (dto.PointsDelta > 0)
            {
                account.LifetimePoints += dto.PointsDelta;
                account.LastAccrualAtUtc = now;
            }
            else
            {
                account.LifetimePoints += dto.PointsDelta;
            }

            var transaction = new LoyaltyPointsTransaction
            {
                LoyaltyAccountId = account.Id,
                BusinessId = account.BusinessId,
                Type = LoyaltyPointsTransactionType.Adjustment,
                PointsDelta = dto.PointsDelta,
                BusinessLocationId = dto.BusinessLocationId,
                PerformedByUserId = dto.PerformedByUserId,
                Reference = dto.Reference,
                Notes = dto.Reason
            };

            _db.Set<LoyaltyPointsTransaction>().Add(transaction);

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            var resultDto = new AdjustLoyaltyPointsResultDto
            {
                LoyaltyAccountId = account.Id,
                TransactionId = transaction.Id,
                NewPointsBalance = account.PointsBalance,
                NewLifetimePoints = account.LifetimePoints
            };

            return Result<AdjustLoyaltyPointsResultDto>.Ok(resultDto);
        }
    }
}
