using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Validators;
using Darwin.Domain.Entities.Catalog;
using FluentValidation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Creates a new add-on group aggregate with nested options/values.
    /// </summary>
    public sealed class CreateAddOnGroupHandler
    {
        private readonly IAppDbContext _db;
        private readonly AddOnGroupCreateValidator _validator = new();

        public CreateAddOnGroupHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(AddOnGroupCreateDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid) throw new ValidationException(vr.Errors);

            var g = new AddOnGroup
            {
                Name = dto.Name.Trim(),
                Currency = dto.Currency.Trim(),
                IsGlobal = dto.IsGlobal,
                SelectionMode = dto.SelectionMode,
                MinSelections = dto.MinSelections,
                MaxSelections = dto.MaxSelections,
                IsActive = dto.IsActive,
                Translations = dto.Translations.Select(t => new AddOnGroupTranslation
                {
                    Culture = t.Culture.Trim(),
                    Name = t.Name.Trim()
                }).ToList(),
                Options = dto.Options.Select(o => new AddOnOption
                {
                    Label = o.Label.Trim(),
                    SortOrder = o.SortOrder,
                    Translations = o.Translations.Select(t => new AddOnOptionTranslation
                    {
                        Culture = t.Culture.Trim(),
                        Label = t.Label.Trim()
                    }).ToList(),
                    Values = o.Values.Select(v => new AddOnOptionValue
                    {
                        Label = v.Label.Trim(),
                        PriceDeltaMinor = v.PriceDeltaMinor,
                        Hint = string.IsNullOrWhiteSpace(v.Hint) ? null : v.Hint.Trim(),
                        SortOrder = v.SortOrder,
                        IsActive = v.IsActive,
                        Translations = v.Translations.Select(t => new AddOnOptionValueTranslation
                        {
                            Culture = t.Culture.Trim(),
                            Label = t.Label.Trim()
                        }).ToList()
                    }).ToList()
                }).ToList()
            };

            _db.Set<AddOnGroup>().Add(g);
            await _db.SaveChangesAsync(ct);
        }
    }
}
