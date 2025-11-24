using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Validators;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Soft deletes a <see cref="LoyaltyRewardTier"/> (user-managed entity).
    /// </summary>
    public sealed class SoftDeleteLoyaltyRewardTierHandler
    {
        private readonly IAppDbContext _db;
        private readonly LoyaltyRewardTierDeleteValidator _validator = new();

        public SoftDeleteLoyaltyRewardTierHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<Result> HandleAsync(LoyaltyRewardTierDeleteDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid) return Result.Fail("Invalid delete request.");

            var entity = await _db.Set<LoyaltyRewardTier>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (entity is null)
                return Result.Fail("Reward tier not found.");

            if (entity.IsDeleted)
                return Result.Ok();

            if (!entity.RowVersion.SequenceEqual(dto.RowVersion ?? Array.Empty<byte>()))
                return Result.Fail("Concurrency conflict. The tier was modified by another process.");

            entity.IsDeleted = true;
            await _db.SaveChangesAsync(ct);

            return Result.Ok();
        }
    }
}
