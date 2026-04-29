using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Performs a hard-replace of variant links for an add-on group.
    /// Mirrors the Brand attach behavior to keep consistency across Admin attach pages:
    /// - Validates group + concurrency (RowVersion)
    /// - Validates Variant IDs
    /// - Physically deletes all existing links (including soft-deleted) and inserts the requested set.
    /// </summary>
    public sealed class AttachAddOnGroupToVariantsHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<AddOnGroupAttachToVariantsDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        /// <summary>
        /// Creates a new handler instance.
        /// </summary>
        public AttachAddOnGroupToVariantsHandler(
            IAppDbContext db,
            IValidator<AddOnGroupAttachToVariantsDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Replaces the set of attached variants for the specified add-on group (hard delete + insert).
        /// </summary>
        public async Task<Result> HandleAsync(AddOnGroupAttachToVariantsDto dto, CancellationToken ct = default)
        {
            // 1) Validate input
            await _validator.ValidateAndThrowAsync(dto, ct);

            // 2) Load group (existence + concurrency)
            var group = await _db.Set<AddOnGroup>()
                .Where(g => g.Id == dto.AddOnGroupId && !g.IsDeleted)
                .Select(g => new { g.Id, g.RowVersion })
                .FirstOrDefaultAsync(ct);

            if (group is null)
                return Result.Fail(_localizer["AddOnGroupNotFound"]);

            var currentVersion = group.RowVersion ?? Array.Empty<byte>();
            if (dto.RowVersion is null || dto.RowVersion.Length == 0 || !currentVersion.SequenceEqual(dto.RowVersion))
                return Result.Fail(_localizer["AddOnGroupConcurrencyConflict"]);

            // 3) Normalize and validate requested variants
            var requested = (dto.VariantIds ?? Array.Empty<Guid>())
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();

            if (requested.Length == 0)
            {
                var existingNone = await _db.Set<AddOnGroupVariant>()
                    .IgnoreQueryFilters()
                    .Where(x => x.AddOnGroupId == group.Id)
                    .ToListAsync(ct);

                if (existingNone.Count > 0)
                {
                    _db.Set<AddOnGroupVariant>().RemoveRange(existingNone);
                    var emptyResult = await SaveChangesOrConcurrencyConflictAsync(ct).ConfigureAwait(false);
                    if (!emptyResult.Succeeded)
                    {
                        return emptyResult;
                    }
                }
                return Result.Ok();
            }

            var validVariantIds = await _db.Set<ProductVariant>()
                .AsNoTracking()
                .Where(v => requested.Contains(v.Id) && !v.IsDeleted)
                .Select(v => v.Id)
                .ToListAsync(ct);

            if (validVariantIds.Count != requested.Length)
                return Result.Fail(_localizer["VariantsNotFoundOrDeleted"]);

            // 4) Hard delete ALL existing links (active + soft-deleted)
            var existing = await _db.Set<AddOnGroupVariant>()
                .IgnoreQueryFilters()
                .Where(x => x.AddOnGroupId == group.Id)
                .ToListAsync(ct);

            if (existing.Count > 0)
                _db.Set<AddOnGroupVariant>().RemoveRange(existing);

            // 5) Insert the requested set
            var toAdd = validVariantIds.Select(vid => new AddOnGroupVariant
            {
                AddOnGroupId = group.Id,
                VariantId = vid
            });

            _db.Set<AddOnGroupVariant>().AddRange(toAdd);

            // 6) Commit
            var result = await SaveChangesOrConcurrencyConflictAsync(ct).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return result;
            }

            return Result.Ok();
        }

        private async Task<Result> SaveChangesOrConcurrencyConflictAsync(CancellationToken ct)
        {
            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return Result.Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["AddOnGroupConcurrencyConflict"]);
            }
        }
    }
}
