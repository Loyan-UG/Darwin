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
    /// Updates an existing <see cref="LoyaltyProgram"/> with optimistic concurrency.
    /// </summary>
    public sealed class UpdateLoyaltyProgramHandler
    {
        private readonly IAppDbContext _db;
        private readonly LoyaltyProgramEditValidator _validator = new();

        public UpdateLoyaltyProgramHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task HandleAsync(LoyaltyProgramEditDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid) throw new ValidationException(vr.Errors);

            var entity = await _db.Set<LoyaltyProgram>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (entity is null || entity.IsDeleted)
                throw new ValidationException("Loyalty program not found.");

            if (!entity.RowVersion.SequenceEqual(dto.RowVersion ?? Array.Empty<byte>()))
                throw new ValidationException("Concurrency conflict. The program was modified by another process.");

            entity.Name = dto.Name.Trim();
            entity.AccrualMode = dto.AccrualMode;
            entity.PointsPerCurrencyUnit = dto.PointsPerCurrencyUnit;
            entity.IsActive = dto.IsActive;
            entity.RulesJson = dto.RulesJson;

            await _db.SaveChangesAsync(ct);
        }
    }
}
