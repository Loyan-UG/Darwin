using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Validators;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Creates a new <see cref="LoyaltyProgram"/> for a business.
    /// </summary>
    public sealed class CreateLoyaltyProgramHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<LoyaltyProgramCreateDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public CreateLoyaltyProgramHandler(
            IAppDbContext db,
            IValidator<LoyaltyProgramCreateDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Creates the program and returns its identifier.
        /// </summary>
        public async Task<Guid> HandleAsync(LoyaltyProgramCreateDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid) throw new ValidationException(vr.Errors);

            // MVP: one program per business. Block duplicates.
            bool exists = await _db.Set<LoyaltyProgram>()
                .AnyAsync(x => x.BusinessId == dto.BusinessId && !x.IsDeleted, ct);

            if (exists)
                throw new ValidationException(_localizer["LoyaltyProgramAlreadyExistsForBusiness"]);

            var entity = new LoyaltyProgram
            {
                BusinessId = dto.BusinessId,
                Name = dto.Name.Trim(),
                AccrualMode = dto.AccrualMode,
                PointsPerCurrencyUnit = dto.PointsPerCurrencyUnit,
                IsActive = dto.IsActive,
                RulesJson = dto.RulesJson
            };

            _db.Set<LoyaltyProgram>().Add(entity);
            await _db.SaveChangesAsync(ct);

            return entity.Id;
        }
    }
}
