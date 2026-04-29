using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Validators;
using Darwin.Domain.Entities.Loyalty;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Updates an existing <see cref="LoyaltyRewardTier"/> with optimistic concurrency.
    /// </summary>
    public sealed class UpdateLoyaltyRewardTierHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<LoyaltyRewardTierEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateLoyaltyRewardTierHandler(
            IAppDbContext db,
            IValidator<LoyaltyRewardTierEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(LoyaltyRewardTierEditDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid) throw new ValidationException(vr.Errors);

            var entity = await _db.Set<LoyaltyRewardTier>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct);

            if (entity is null)
                throw new ValidationException(_localizer["RewardTierNotFound"]);

            var programExists = await _db.Set<LoyaltyProgram>()
                .AnyAsync(x => x.Id == dto.LoyaltyProgramId && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (!programExists)
                throw new ValidationException(_localizer["LoyaltyProgramNotFound"]);

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
                throw new ValidationException(_localizer["ConcurrencyConflictRewardTierModified"]);

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
