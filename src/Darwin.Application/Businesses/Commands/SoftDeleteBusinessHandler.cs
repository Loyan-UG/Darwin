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
using Microsoft.Extensions.Localization;

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
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public SoftDeleteBusinessHandler(
            IAppDbContext db,
            IValidator<BusinessDeleteDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(BusinessDeleteDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid)
                return Result.Fail(_localizer["InvalidDeleteRequest"]);

            var entity = await _db.Set<Business>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (entity is null)
                return Result.Fail(_localizer["BusinessNotFound"]);

            if (entity.IsDeleted)
                return Result.Ok();

            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            var requestVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(requestVersion))
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);

            entity.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
