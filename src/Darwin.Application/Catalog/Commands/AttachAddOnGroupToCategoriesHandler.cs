using Darwin.Application;
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
    /// Attaches an add-on group to categories with optimistic concurrency protection.
    /// </summary>
    public sealed class AttachAddOnGroupToCategoriesHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<AddOnGroupAttachToCategoriesDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public AttachAddOnGroupToCategoriesHandler(
            IAppDbContext db,
            IValidator<AddOnGroupAttachToCategoriesDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(AddOnGroupAttachToCategoriesDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var group = await _db.Set<AddOnGroup>()
                .Where(g => g.Id == dto.AddOnGroupId && !g.IsDeleted)
                .Select(g => new { g.Id, g.RowVersion })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            if (group is null)
                return Result.Fail(_localizer["AddOnGroupNotFound"]);

            var currentVersion = group.RowVersion ?? Array.Empty<byte>();
            if (dto.RowVersion.Length == 0 || !currentVersion.SequenceEqual(dto.RowVersion))
                return Result.Fail(_localizer["AddOnGroupConcurrencyConflict"]);

            var requested = (dto.CategoryIds ?? Array.Empty<Guid>())
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();

            var validCategoryIds = await _db.Set<Category>()
                .AsNoTracking()
                .Where(c => requested.Contains(c.Id) && !c.IsDeleted)
                .Select(c => c.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (validCategoryIds.Count != requested.Length)
            {
                return Result.Fail(_localizer["CategoriesNotFoundOrDeleted"]);
            }

            var existing = await _db.Set<AddOnGroupCategory>()
                .IgnoreQueryFilters()
                .Where(x => x.AddOnGroupId == group.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            _db.Set<AddOnGroupCategory>().RemoveRange(existing);
            _db.Set<AddOnGroupCategory>().AddRange(validCategoryIds.Select(cid => new AddOnGroupCategory
            {
                AddOnGroupId = group.Id,
                CategoryId = cid
            }));

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
