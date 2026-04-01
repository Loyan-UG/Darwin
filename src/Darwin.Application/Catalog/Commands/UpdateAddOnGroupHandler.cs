using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Validators;
using Darwin.Domain.Entities.Catalog;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Replaces an add-on group's editable fields and fully rewrites its nested options/values (simple upsert strategy).
    /// </summary>
    public sealed class UpdateAddOnGroupHandler
    {
        private readonly IAppDbContext _db;
        private readonly AddOnGroupEditValidator _validator = new();
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateAddOnGroupHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task HandleAsync(AddOnGroupEditDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid) throw new ValidationException(vr.Errors);

            var g = await _db.Set<AddOnGroup>()
                .Include(x => x.Options).ThenInclude(o => o.Values)
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct);

            if (g == null) throw new InvalidOperationException(_localizer["AddOnGroupNotFound"]);

            if (!g.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);

            g.Name = dto.Name.Trim();
            g.Currency = dto.Currency.Trim();
            g.IsGlobal = dto.IsGlobal;
            g.SelectionMode = dto.SelectionMode;
            g.MinSelections = dto.MinSelections;
            g.MaxSelections = dto.MaxSelections;
            g.IsActive = dto.IsActive;

            g.Options.Clear();
            foreach (var o in dto.Options.OrderBy(x => x.SortOrder))
            {
                var opt = new AddOnOption
                {
                    AddOnGroupId = g.Id,
                    Label = o.Label.Trim(),
                    SortOrder = o.SortOrder
                };
                foreach (var v in o.Values.OrderBy(x => x.SortOrder))
                {
                    opt.Values.Add(new AddOnOptionValue
                    {
                        Label = v.Label.Trim(),
                        PriceDeltaMinor = v.PriceDeltaMinor,
                        Hint = string.IsNullOrWhiteSpace(v.Hint) ? null : v.Hint.Trim(),
                        SortOrder = v.SortOrder,
                        IsActive = v.IsActive
                    });
                }
                g.Options.Add(opt);
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
