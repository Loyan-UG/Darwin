using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Commands
{
    /// <summary>
    /// Soft-deletes a <see cref="BusinessLocation"/> (Admin-managed entity).
    /// </summary>
    public sealed class SoftDeleteBusinessLocationHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<BusinessLocationDeleteDto> _validator;

        public SoftDeleteBusinessLocationHandler(IAppDbContext db, IValidator<BusinessLocationDeleteDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Result> HandleAsync(BusinessLocationDeleteDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid)
                return Result.Fail("Invalid delete request.");

            var entity = await _db.Set<BusinessLocation>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (entity is null)
                return Result.Fail("Business location not found.");

            if (entity.IsDeleted)
                return Result.Ok();

            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            var requestVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(requestVersion))
                return Result.Fail("Concurrency conflict. The item was modified by another process.");

            entity.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
