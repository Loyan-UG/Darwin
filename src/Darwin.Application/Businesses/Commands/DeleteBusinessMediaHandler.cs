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
    /// Hard-deletes a <see cref="BusinessMedia"/> row.
    /// Intended for logic-managed / join-like data.
    /// </summary>
    public sealed class DeleteBusinessMediaHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<BusinessMediaDeleteDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public DeleteBusinessMediaHandler(
            IAppDbContext db,
            IValidator<BusinessMediaDeleteDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(BusinessMediaDeleteDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var entity = await _db.Set<BusinessMedia>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return Result.Fail(_localizer["BusinessMediaNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            var remaining = await _db.Set<BusinessMedia>()
                .Where(x => x.BusinessId == entity.BusinessId && x.Id != entity.Id && !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (remaining.Count == 0)
            {
                return Result.Fail(_localizer["BusinessMediaAtLeastOneImageRequired"]);
            }

            if (entity.IsPrimary)
            {
                remaining[0].IsPrimary = true;
            }

            _db.Set<BusinessMedia>().Remove(entity);
            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            return Result.Ok();
        }

        public async Task HandleAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Set<BusinessMedia>()
                .FirstOrDefaultAsync(x => x.Id == id, ct)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return;
            }

            _db.Set<BusinessMedia>().Remove(entity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
