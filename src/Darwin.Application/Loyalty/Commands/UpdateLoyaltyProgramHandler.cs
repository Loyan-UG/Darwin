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
    /// Updates an existing <see cref="LoyaltyProgram"/> with optimistic concurrency.
    /// </summary>
    public sealed class UpdateLoyaltyProgramHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<LoyaltyProgramEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateLoyaltyProgramHandler(
            IAppDbContext db,
            IValidator<LoyaltyProgramEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(LoyaltyProgramEditDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid) throw new ValidationException(vr.Errors);

            var entity = await _db.Set<LoyaltyProgram>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct);

            if (entity is null)
                throw new ValidationException(_localizer["LoyaltyProgramNotFound"]);

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
                throw new ValidationException(_localizer["ConcurrencyConflictProgramModified"]);

            entity.Name = dto.Name.Trim();
            entity.AccrualMode = dto.AccrualMode;
            entity.PointsPerCurrencyUnit = dto.PointsPerCurrencyUnit;
            entity.IsActive = dto.IsActive;
            entity.RulesJson = dto.RulesJson;

            await _db.SaveChangesAsync(ct);
        }
    }
}
