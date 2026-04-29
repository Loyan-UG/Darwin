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
    /// Attaches an add-on group to brands with optimistic concurrency protection.
    /// </summary>
    public sealed class AttachAddOnGroupToBrandsHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<AddOnGroupAttachToBrandsDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public AttachAddOnGroupToBrandsHandler(
            IAppDbContext db,
            IValidator<AddOnGroupAttachToBrandsDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(AddOnGroupAttachToBrandsDto dto, CancellationToken ct = default)
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

            var requested = (dto.BrandIds ?? Array.Empty<Guid>())
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();

            var validBrandIds = await _db.Set<Brand>()
                .AsNoTracking()
                .Where(b => requested.Contains(b.Id) && !b.IsDeleted)
                .Select(b => b.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (validBrandIds.Count != requested.Length)
            {
                return Result.Fail(_localizer["BrandsNotFoundOrDeleted"]);
            }

            var existing = await _db.Set<AddOnGroupBrand>()
                .IgnoreQueryFilters()
                .Where(x => x.AddOnGroupId == group.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            _db.Set<AddOnGroupBrand>().RemoveRange(existing);
            _db.Set<AddOnGroupBrand>().AddRange(validBrandIds.Select(bid => new AddOnGroupBrand
            {
                AddOnGroupId = group.Id,
                BrandId = bid
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
