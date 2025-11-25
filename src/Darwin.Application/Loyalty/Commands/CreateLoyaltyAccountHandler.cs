using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Creates a loyalty account for (BusinessId, UserId). 
    /// Must not create duplicates. 
    /// Matches the command pattern used throughout Darwin.
    /// </summary>
    public sealed class CreateLoyaltyAccountHandler
    {
        private readonly IAppDbContext _db;

        public CreateLoyaltyAccountHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Creates the account only when not already existing.
        /// </summary>
        public async Task<Result<Guid>> HandleAsync(GetOrCreateLoyaltyAccountDto dto, CancellationToken ct = default)
        {
            if (dto.BusinessId == Guid.Empty || dto.UserId == Guid.Empty)
                return Result<Guid>.Fail("Invalid BusinessId or UserId.");

            var exists = await _db.Set<LoyaltyAccount>()
                .AnyAsync(a => a.BusinessId == dto.BusinessId && a.UserId == dto.UserId, ct);

            if (exists)
                return Result<Guid>.Fail("Loyalty account already exists.");

            var entity = new LoyaltyAccount
            {
                BusinessId = dto.BusinessId,
                UserId = dto.UserId,
                Status = LoyaltyAccountStatus.Active
            };

            _db.Set<LoyaltyAccount>().Add(entity);
            await _db.SaveChangesAsync(ct);

            return Result<Guid>.Ok(entity.Id);
        }
    }
}
