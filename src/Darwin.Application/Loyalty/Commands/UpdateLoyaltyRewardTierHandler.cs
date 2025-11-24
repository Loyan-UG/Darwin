using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Validators;
using Darwin.Domain.Entities.Loyalty;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Updates an existing <see cref="LoyaltyRewardTier"/> with optimistic concurrency.
    /// </summary>
    public sealed class UpdateLoyaltyRewardTierHandler
    {
        private readonly IAppDbContext _db;
        private readonly LoyaltyRewardTierEditValidator _validator = new();

        public UpdateLoyaltyRewardTierHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task HandleAsync(LoyaltyRewardTierEditDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid) throw new ValidationException(vr.Errors);

            var entity = await _db.Set<LoyaltyRewardTier>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (entity is null || entity.IsDeleted)
                throw new ValidationException("Reward tier not found.");

            if (!entity.RowVersion.SequenceEqual(dto.RowVersion ?? Array.Empty<byte>()))
                throw new ValidationException("Concurrency conflict. The tier was modified by another process.");

            entity.LoyaltyProgramId = dto.LoyaltyProgramId;
            entity.PointsRequired = dto.PointsRequired;
            entity.RewardType = dto.RewardType;
            entity.RewardValue = dto.RewardValue;
            entity.Description = dto.Description;
            entity.AllowSelfRedemption = dto.AllowSelfRedemption;
            entity.MetadataJson = dto.MetadataJson;

            await _db.SaveChangesAsync(ct);
        }
    }
}
