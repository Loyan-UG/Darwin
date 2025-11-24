using System;
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
    /// Creates a new <see cref="LoyaltyRewardTier"/> for a program.
    /// </summary>
    public sealed class CreateLoyaltyRewardTierHandler
    {
        private readonly IAppDbContext _db;
        private readonly LoyaltyRewardTierCreateValidator _validator = new();

        public CreateLoyaltyRewardTierHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<Guid> HandleAsync(LoyaltyRewardTierCreateDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid) throw new ValidationException(vr.Errors);

            bool programExists = await _db.Set<LoyaltyProgram>()
                .AnyAsync(x => x.Id == dto.LoyaltyProgramId && !x.IsDeleted, ct);

            if (!programExists)
                throw new ValidationException("Loyalty program not found.");

            var entity = new LoyaltyRewardTier
            {
                LoyaltyProgramId = dto.LoyaltyProgramId,
                PointsRequired = dto.PointsRequired,
                RewardType = dto.RewardType,
                RewardValue = dto.RewardValue,
                Description = dto.Description,
                AllowSelfRedemption = dto.AllowSelfRedemption,
                MetadataJson = dto.MetadataJson
            };

            _db.Set<LoyaltyRewardTier>().Add(entity);
            await _db.SaveChangesAsync(ct);

            return entity.Id;
        }
    }
}
