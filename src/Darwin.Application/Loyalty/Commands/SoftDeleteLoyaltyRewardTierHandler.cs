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
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Soft deletes a <see cref="LoyaltyRewardTier"/> (user-managed entity).
    /// </summary>
    public sealed class SoftDeleteLoyaltyRewardTierHandler
    {
        private readonly IAppDbContext _db;
        private readonly LoyaltyRewardTierDeleteValidator _validator = new();
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public SoftDeleteLoyaltyRewardTierHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(LoyaltyRewardTierDeleteDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid) return Result.Fail(_localizer["InvalidDeleteRequest"]);

            var entity = await _db.Set<LoyaltyRewardTier>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (entity is null)
                return Result.Fail(_localizer["LoyaltyRewardTierNotFound"]);

            if (entity.IsDeleted)
                return Result.Ok();

            var rowVersion = dto.RowVersion;
            if (rowVersion is { Length: > 0 })
            {
                var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
                if (!currentVersion.SequenceEqual(rowVersion))
                    return Result.Fail(_localizer["LoyaltyRewardTierConcurrencyConflict"]);
            }

            entity.IsDeleted = true;
            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["LoyaltyRewardTierConcurrencyConflict"]);
            }

            return Result.Ok();
        }
    }
}
