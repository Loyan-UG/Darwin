using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
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
    /// Command handler that activates a previously suspended loyalty account.
    /// Once activated, the account can participate in accrual and redemption
    /// flows again.
    /// </summary>
    public sealed class ActivateLoyaltyAccountHandler
    {
        private readonly IAppDbContext _db;
        private readonly ActivateLoyaltyAccountValidator _validator = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivateLoyaltyAccountHandler"/> class.
        /// </summary>
        /// <param name="db">Application database abstraction.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="db"/> is null.</exception>
        public ActivateLoyaltyAccountHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Activates the specified loyalty account if it exists and is not already active.
        /// </summary>
        /// <param name="dto">Request containing the account id and optional row version.</param>
        /// <param name="ct">Cancellation token used to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Result"/> indicating success or failure of the operation.
        /// </returns>
        public async Task<Result> HandleAsync(ActivateLoyaltyAccountDto dto, CancellationToken ct = default)
        {
            var validationResult = _validator.Validate(dto);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var account = await _db.Set<LoyaltyAccount>()
                .FirstOrDefaultAsync(a => a.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (account is null)
            {
                return Result.Fail("Loyalty account not found.");
            }

            // Optional optimistic concurrency check.
            if (dto.RowVersion is not null &&
                !account.RowVersion.SequenceEqual(dto.RowVersion))
            {
                return Result.Fail("Concurrency conflict. The loyalty account was modified by another process.");
            }

            if (account.Status == LoyaltyAccountStatus.Active)
            {
                // Idempotent behavior: if already active, treat as success.
                return Result.Ok();
            }

            account.Status = LoyaltyAccountStatus.Active;

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            return Result.Ok();
        }
    }
}
