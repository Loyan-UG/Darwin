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
    /// Soft-deletes a <see cref="Business"/> by setting IsDeleted = true.
    /// Idempotent and concurrency-safe (RowVersion).
    /// </summary>
    public sealed class SoftDeleteBusinessHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<BusinessDeleteDto> _validator;

        public SoftDeleteBusinessHandler(IAppDbContext db, IValidator<BusinessDeleteDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Result> HandleAsync(BusinessDeleteDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid)
                return Result.Fail("Invalid delete request.");

            var entity = await _db.Set<Business>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (entity is null)
                return Result.Fail("Business not found.");

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
